// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator;

/// <summary>
/// Incremental source generator that emits deterministic, versioned JSON API schemas
/// for all public types in the compilation.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ApiSchemaGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Read MSBuild properties via AnalyzerConfig
        var msbuildConfig = context.AnalyzerConfigOptionsProvider.Select(
            static (provider, ct) =>
            {
                var config = new AssemblyConfig();

                if (provider.GlobalOptions.TryGetValue("build_property.AISchemaEmitStandard", out var emitStd))
                    config.EmitStandard = string.Equals(emitStd, "true", StringComparison.OrdinalIgnoreCase);

                if (provider.GlobalOptions.TryGetValue("build_property.AISchemaEmitVendor", out var emitVendor))
                    config.EmitVendor = string.Equals(emitVendor, "true", StringComparison.OrdinalIgnoreCase);

                if (provider.GlobalOptions.TryGetValue("build_property.AISchemaVendorFolder", out var vendorFolder) &&
                    !string.IsNullOrEmpty(vendorFolder))
                    config.VendorFolder = vendorFolder;

                if (provider.GlobalOptions.TryGetValue("build_property.AISchemaSign", out var sign))
                    config.Sign = string.Equals(sign, "true", StringComparison.OrdinalIgnoreCase);

                if (provider.GlobalOptions.TryGetValue("build_property.AISchemaSigningPrivateKey", out var signingKey) &&
                    !string.IsNullOrEmpty(signingKey))
                    config.SigningKeyId = signingKey;

                if (provider.GlobalOptions.TryGetValue("build_property.AISchemaIncludeInternals", out var includeInternals))
                    config.IncludeInternals = string.Equals(includeInternals, "true", StringComparison.OrdinalIgnoreCase);

                return config;
            });

        // Collect all named type symbols from the compilation
        var compilationWithConfig = context.CompilationProvider.Combine(msbuildConfig);

        var typeSymbols = compilationWithConfig.Select(
            static (pair, ct) =>
            {
                var (compilation, config) = pair;
                var types = new List<INamedTypeSymbol>();
                CollectTypes(compilation.GlobalNamespace, types, compilation, config.IncludeInternals, ct);
                return types.ToImmutableArray();
            });

        // Collect assembly-level config (merges with MSBuild config)
        var assemblyConfig = compilationWithConfig.Select(
            static (pair, ct) =>
            {
                var (compilation, msbuildConfig) = pair;
                var attrConfig = ConfigExtractor.ExtractConfig(compilation.Assembly);
                return MergeConfig(attrConfig, msbuildConfig);
            });

        // Combine and generate
        var combined = typeSymbols.Combine(assemblyConfig)
            .Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (spc, data) =>
        {
            var ((types, config), compilation) = data;
            Execute(spc, compilation, types, config);
        });
    }

    private static AssemblyConfig MergeConfig(AssemblyConfig attrConfig, AssemblyConfig msbuildConfig)
    {
        // Attribute config takes precedence where explicitly set;
        // MSBuild config provides defaults
        return new AssemblyConfig
        {
            OutputFolder = attrConfig.OutputFolder,
            EmitStandard = attrConfig.EmitStandard || msbuildConfig.EmitStandard,
            EmitVendor = attrConfig.EmitVendor || msbuildConfig.EmitVendor,
            VendorFolder = attrConfig.VendorFolder ?? msbuildConfig.VendorFolder,
            Sign = attrConfig.Sign || msbuildConfig.Sign,
            SigningKeyId = attrConfig.SigningKeyId ?? msbuildConfig.SigningKeyId,
            IncludeInternals = attrConfig.IncludeInternals || msbuildConfig.IncludeInternals,
        };
    }

    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<INamedTypeSymbol> types,
        AssemblyConfig config)
    {
        if (types.IsDefaultOrEmpty)
        {
            return;
        }

        var assemblyName = compilation.AssemblyName ?? "Unknown";
        var assemblyVersion = compilation.Assembly.Identity.Version.ToString();

        // Build canonical model
        var modelBuilder = new CanonicalModelBuilder(compilation);
        var typeModels = modelBuilder.BuildTypes(types);

        // Compute deterministic hash
        var canonicalJson = CanonicalSerializer.SerializeForHashing(typeModels);
        var apiHash = HashComputer.ComputeSha256(canonicalJson);

        // Build assembly schema JSON
        var schemaJson = SchemaEmitter.EmitAssemblySchema(
            assemblyName,
            assemblyVersion,
            compilation.Options is CSharpCompilationOptions csharpOptions
                ? $"net{(csharpOptions.Platform == Platform.AnyCpu ? "10.0" : "10.0")}"
                : "net10.0",
            typeModels,
            apiHash,
            config);

        // Emit as additional file via source text
        context.AddSource(
            $"{assemblyName}.ai-schema.g.cs",
            SourceText.From(
                $$"""
                // <auto-generated/>
                // API Schema generated by ApiContracts.Generator
                // Assembly: {{assemblyName}}
                // Hash: {{apiHash}}
                // This file is generated and should not be edited manually.

                #if false
                // The JSON schema is emitted as an embedded resource.
                // See: ai-skills/apis/reference/{{assemblyName}}.ai-schema.json
                #endif
                """,
                Encoding.UTF8));

        // Emit the actual JSON schema as additional source
        context.AddSource(
            $"{assemblyName}.ai-schema.json.g.cs",
            SourceText.From(
                GenerateSchemaEmbedder(assemblyName, schemaJson),
                Encoding.UTF8));
    }

    private static string GenerateSchemaEmbedder(string assemblyName, string schemaJson)
    {
        var escaped = schemaJson.Replace("\"", "\"\"");
        return $$"""
            // <auto-generated/>
            namespace ApiContracts.Generated
            {
                internal static partial class EmbeddedSchemas
                {
                    /// <summary>
                    /// Gets the generated AI schema JSON for the {{assemblyName}} assembly.
                    /// </summary>
                    public static string {{SanitizeIdentifier(assemblyName)}}Schema => @"{{escaped}}";
                }
            }
            """;
    }

    private static string SanitizeIdentifier(string name) =>
        name.Replace(".", "").Replace("-", "").Replace(" ", "");

    private static void CollectTypes(
        INamespaceSymbol ns,
        List<INamedTypeSymbol> types,
        Compilation compilation,
        bool includeInternals,
        System.Threading.CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        foreach (var type in ns.GetTypeMembers())
        {
            var isAccessible = type.DeclaredAccessibility == Accessibility.Public ||
                (includeInternals && type.DeclaredAccessibility == Accessibility.Internal);

            if (isAccessible && !IsCompilerGenerated(type))
            {
                types.Add(type);
            }
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            if (SymbolEqualityComparer.Default.Equals(childNs.ContainingAssembly, compilation.Assembly))
            {
                CollectTypes(childNs, types, compilation, includeInternals, ct);
            }
        }
    }

    private static bool IsCompilerGenerated(INamedTypeSymbol type)
    {
        return type.Name.StartsWith("<") ||
               type.GetAttributes().Any(a =>
                   a.AttributeClass?.Name == "CompilerGeneratedAttribute");
    }
}
