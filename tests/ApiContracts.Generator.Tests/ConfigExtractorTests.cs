// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator.Tests;

public class ConfigExtractorTests
{
    private const string ConfigAttributeSource = """
        namespace ApiContracts
        {
            [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = false)]
            public sealed class ApiContractConfigAttribute : System.Attribute
            {
                public string OutputFolder { get; set; } = "ai-skills/apis";
                public bool EmitStandard { get; set; } = true;
                public bool EmitVendor { get; set; }
                public string VendorFolder { get; set; }
                public bool Sign { get; set; }
                public string SigningKeyId { get; set; }
                public bool IncludeInternals { get; set; }
            }
        }
        """;

    [Fact]
    public void ExtractConfig_NoAttribute_ReturnsDefaults()
    {
        // When there's no ApiContractConfigAttribute, defaults should be used
        var config = new AssemblyConfig();

        Assert.Equal("ai-skills/apis", config.OutputFolder);
        Assert.True(config.EmitStandard);
        Assert.False(config.EmitVendor);
        Assert.Null(config.VendorFolder);
        Assert.False(config.Sign);
        Assert.Null(config.SigningKeyId);
        Assert.False(config.IncludeInternals);
    }

    [Fact]
    public void ExtractConfig_NoAttribute_FromCompilation_ReturnsDefaults()
    {
        var source = """
            namespace TestNs
            {
                public class Foo { }
            }
            """;

        var compilation = CreateCompilation(source);
        var config = ConfigExtractor.ExtractConfig(compilation.Assembly);

        Assert.Equal("ai-skills/apis", config.OutputFolder);
        Assert.True(config.EmitStandard);
        Assert.False(config.EmitVendor);
        Assert.Null(config.VendorFolder);
        Assert.False(config.Sign);
        Assert.Null(config.SigningKeyId);
        Assert.False(config.IncludeInternals);
    }

    [Fact]
    public void ExtractConfig_WithFullAttribute_ExtractsAllValues()
    {
        var source = """
            using ApiContracts;

            [assembly: ApiContractConfig(
                OutputFolder = "custom/output",
                EmitStandard = false,
                EmitVendor = true,
                VendorFolder = "vendor/out",
                Sign = true,
                SigningKeyId = "my-key-id",
                IncludeInternals = true)]

            namespace TestNs
            {
                public class Foo { }
            }
            """;

        var compilation = CreateCompilation(source);
        var config = ConfigExtractor.ExtractConfig(compilation.Assembly);

        Assert.Equal("custom/output", config.OutputFolder);
        Assert.False(config.EmitStandard);
        Assert.True(config.EmitVendor);
        Assert.Equal("vendor/out", config.VendorFolder);
        Assert.True(config.Sign);
        Assert.Equal("my-key-id", config.SigningKeyId);
        Assert.True(config.IncludeInternals);
    }

    [Fact]
    public void ExtractConfig_PartialAttribute_MixesWithDefaults()
    {
        var source = """
            using ApiContracts;

            [assembly: ApiContractConfig(EmitVendor = true, VendorFolder = "my-vendor")]

            namespace TestNs
            {
                public class Foo { }
            }
            """;

        var compilation = CreateCompilation(source);
        var config = ConfigExtractor.ExtractConfig(compilation.Assembly);

        // Explicitly set
        Assert.True(config.EmitVendor);
        Assert.Equal("my-vendor", config.VendorFolder);

        // Defaults
        Assert.Equal("ai-skills/apis", config.OutputFolder);
        Assert.True(config.EmitStandard);
        Assert.False(config.Sign);
        Assert.Null(config.SigningKeyId);
        Assert.False(config.IncludeInternals);
    }

    [Fact]
    public void ExtractConfig_SigningOnly_ExtractsSigningConfig()
    {
        var source = """
            using ApiContracts;

            [assembly: ApiContractConfig(Sign = true, SigningKeyId = "prod-key-2025")]

            namespace TestNs
            {
                public class Foo { }
            }
            """;

        var compilation = CreateCompilation(source);
        var config = ConfigExtractor.ExtractConfig(compilation.Assembly);

        Assert.True(config.Sign);
        Assert.Equal("prod-key-2025", config.SigningKeyId);
        Assert.False(config.EmitVendor);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var attrTree = CSharpSyntaxTree.ParseText(ConfigAttributeSource);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, attrTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
