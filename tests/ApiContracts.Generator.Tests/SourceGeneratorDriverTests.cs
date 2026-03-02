// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace ApiContracts.Generator.Tests;

public class SourceGeneratorDriverTests
{
    private const string ApiContractAttributeSource = """
        namespace ApiContracts
        {
            [System.AttributeUsage(
                System.AttributeTargets.Assembly |
                System.AttributeTargets.Class | System.AttributeTargets.Struct |
                System.AttributeTargets.Interface | System.AttributeTargets.Enum |
                System.AttributeTargets.Method | System.AttributeTargets.Property |
                System.AttributeTargets.Field | System.AttributeTargets.Parameter |
                System.AttributeTargets.Event,
                AllowMultiple = false, Inherited = true)]
            public sealed class ApiContractAttribute : System.Attribute
            {
                public bool Ignore { get; set; }
            }

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
    public void Generator_ProducesTwoSourceOutputs()
    {
        var source = """
            namespace TestNs
            {
                public class Person
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedTrees.Length);
    }

    [Fact]
    public void Generator_ProducesMarkerAndEmbedder()
    {
        var source = """
            namespace TestNs
            {
                public class Widget { public int Id { get; set; } }
            }
            """;

        var result = RunGenerator(source);
        var fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToList();

        Assert.Contains(fileNames, f => f.EndsWith(".api-schema.g.cs"));
        Assert.Contains(fileNames, f => f.EndsWith(".api-schema.json.g.cs"));
    }

    [Fact]
    public void Generator_ProducesNoDiagnostics()
    {
        var source = """
            namespace TestNs
            {
                public class Foo { public string Bar { get; set; } }
            }
            """;

        var result = RunGenerator(source);

        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Generator_OnlyInternalTypes_ExcludedFromSchema()
    {
        var source = """
            namespace TestNs
            {
                internal class Hidden { }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        // The generator still runs (attribute types are public), but
        // our internal type should not appear in the schema
        if (json is not null)
        {
            using var doc = JsonDocument.Parse(json);
            var types = doc.RootElement.GetProperty("types");
            var names = types.EnumerateArray()
                .Select(t => t.GetProperty("name").GetString())
                .ToList();

            Assert.DoesNotContain("Hidden", names);
        }
    }

    [Fact]
    public void Generator_EmbedderContainsValidJson()
    {
        var source = """
            namespace TestNs
            {
                public class Customer
                {
                    public string Name { get; set; }
                    public int Id { get; set; }
                }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        Assert.NotNull(json);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("1.0.0", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.TryGetProperty("apiHash", out _));
        Assert.True(root.TryGetProperty("types", out var types));
        Assert.Equal(JsonValueKind.Array, types.ValueKind);
    }

    [Fact]
    public void Generator_SchemaContainsPackageInfo()
    {
        var source = """
            namespace TestNs
            {
                public class Item { public int Id { get; set; } }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var package = doc.RootElement.GetProperty("package");

        Assert.Equal("TestAssembly", package.GetProperty("name").GetString());
        Assert.True(package.TryGetProperty("version", out _));
        Assert.Equal("net10.0", package.GetProperty("targetFramework").GetString());
    }

    [Fact]
    public void Generator_CapturesClassInSchema()
    {
        var source = """
            namespace TestNs
            {
                public class Order
                {
                    public int Id { get; set; }
                    public decimal Total { get; set; }
                }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");

        var order = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Order");
        Assert.Equal("class", order.GetProperty("kind").GetString());
        Assert.Equal("TestNs.Order", order.GetProperty("fullName").GetString());
        Assert.Equal("TestNs", order.GetProperty("namespace").GetString());

        var members = order.GetProperty("members");
        var memberNames = members.EnumerateArray()
            .Select(m => m.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("Id", memberNames);
        Assert.Contains("Total", memberNames);
    }

    [Fact]
    public void Generator_CapturesInterfaceInSchema()
    {
        var source = """
            namespace TestNs
            {
                public interface IService
                {
                    void Execute();
                }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var svc = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "IService");

        Assert.Equal("interface", svc.GetProperty("kind").GetString());
    }

    [Fact]
    public void Generator_CapturesEnumInSchema()
    {
        var source = """
            namespace TestNs
            {
                public enum Status { Pending = 0, Active = 1, Closed = 2 }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var status = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Status");

        Assert.Equal("enum", status.GetProperty("kind").GetString());
        var enumMembers = status.GetProperty("enumMembers");
        Assert.Equal(3, enumMembers.GetArrayLength());
    }

    [Fact]
    public void Generator_CapturesStructInSchema()
    {
        var source = """
            namespace TestNs
            {
                public struct Point
                {
                    public int X { get; set; }
                    public int Y { get; set; }
                }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var point = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Point");

        Assert.Equal("struct", point.GetProperty("kind").GetString());
    }

    [Fact]
    public void Generator_CapturesRecordInSchema()
    {
        var source = """
            namespace TestNs
            {
                public record Person(string Name, int Age);
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var person = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Person");

        Assert.Equal("record", person.GetProperty("kind").GetString());
    }

    [Fact]
    public void Generator_ExcludesIgnoredTypes()
    {
        var source = """
            using ApiContracts;

            namespace TestNs
            {
                [ApiContract(Ignore = true)]
                public class Secret { }

                public class Visible { public int Id { get; set; } }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var typeNames = types.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.DoesNotContain("Secret", typeNames);
        Assert.Contains("Visible", typeNames);
    }

    [Fact]
    public void Generator_IsDeterministic()
    {
        var source = """
            namespace TestNs
            {
                public class B { public int Id { get; set; } }
                public class A { public string Name { get; set; } }
            }
            """;

        var result1 = RunGenerator(source);
        var result2 = RunGenerator(source);

        var json1 = ExtractEmbeddedJson(result1);
        var json2 = ExtractEmbeddedJson(result2);

        Assert.Equal(json1, json2);
    }

    [Fact]
    public void Generator_XmlDocs_FlowThroughToSchema()
    {
        var source = """
            namespace TestNs
            {
                /// <summary>A customer entity.</summary>
                /// <remarks>Used for billing.</remarks>
                public class Customer
                {
                    /// <summary>Unique identifier.</summary>
                    public int Id { get; set; }
                }
            }
            """;

        var result = RunGenerator(source,
            parseOptions: new CSharpParseOptions(
                languageVersion: LanguageVersion.Preview,
                documentationMode: DocumentationMode.Parse));

        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var customer = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Customer");

        var docs = customer.GetProperty("docs");
        // summary and remarks are now arrays of doc nodes
        var summaryArr = docs.GetProperty("summary");
        Assert.Equal(JsonValueKind.Array, summaryArr.ValueKind);
        var firstSummaryNode = summaryArr.EnumerateArray().First();
        Assert.Equal("text", firstSummaryNode.GetProperty("kind").GetString());
        Assert.Equal("A customer entity.", firstSummaryNode.GetProperty("text").GetString());

        var remarksArr = docs.GetProperty("remarks");
        Assert.Equal(JsonValueKind.Array, remarksArr.ValueKind);
        var firstRemarksNode = remarksArr.EnumerateArray().First();
        Assert.Equal("text", firstRemarksNode.GetProperty("kind").GetString());
        Assert.Equal("Used for billing.", firstRemarksNode.GetProperty("text").GetString());
    }

    [Fact]
    public void Generator_MultipleTypes_AllAppearInSchema()
    {
        var source = """
            namespace TestNs
            {
                public class Alpha { }
                public interface IBeta { }
                public enum Gamma { X, Y }
                public struct Delta { }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var names = types.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("Alpha", names);
        Assert.Contains("IBeta", names);
        Assert.Contains("Gamma", names);
        Assert.Contains("Delta", names);
    }

    [Fact]
    public void Generator_ApiHash_IsConsistentSha256Format()
    {
        var source = """
            namespace TestNs
            {
                public class Foo { public int Bar { get; set; } }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var hash = doc.RootElement.GetProperty("apiHash").GetString();

        Assert.NotNull(hash);
        Assert.StartsWith("sha256:", hash);
        Assert.True(hash!.Length > "sha256:".Length);
    }

    [Fact]
    public void Generator_MarkerFile_ContainsAssemblyNameAndHash()
    {
        var source = """
            namespace TestNs
            {
                public class Test { public int Id { get; set; } }
            }
            """;

        var result = RunGenerator(source);
        var markerTree = result.GeneratedTrees
            .First(t => Path.GetFileName(t.FilePath).EndsWith(".api-schema.g.cs") &&
                        !t.FilePath.Contains(".json"));

        var text = markerTree.GetText().ToString();

        Assert.Contains("TestAssembly", text);
        Assert.Contains("sha256:", text);
        Assert.Contains("<auto-generated/>", text);
    }

    [Fact]
    public void Generator_EmbedderFile_HasSchemaProperty()
    {
        var source = """
            namespace TestNs
            {
                public class Test { public int Id { get; set; } }
            }
            """;

        var result = RunGenerator(source);
        var embedderTree = result.GeneratedTrees
            .First(t => Path.GetFileName(t.FilePath).Contains(".json.g.cs"));

        var text = embedderTree.GetText().ToString();

        Assert.Contains("namespace ApiContracts.Generated", text);
        Assert.Contains("EmbeddedSchemas", text);
        Assert.Contains("Schema", text);
    }

    [Fact]
    public void Generator_GenericType_CapturedCorrectly()
    {
        var source = """
            namespace TestNs
            {
                public class Repository<T> where T : class, new()
                {
                    public T GetById(int id) => default;
                }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var repo = types.EnumerateArray().First(t => t.GetProperty("name").GetString() == "Repository");

        Assert.True(repo.GetProperty("isGeneric").GetBoolean());
        Assert.True(repo.TryGetProperty("genericParameters", out var gps));
        var gp = gps.EnumerateArray().First();
        Assert.Equal("T", gp.GetProperty("name").GetString());
    }

    [Fact]
    public void Generator_IncludesInternals_WhenConfigured()
    {
        var source = """
            namespace TestNs
            {
                public class PublicType { }
                internal class InternalType { }
            }
            """;

        var configOptions = new Dictionary<string, string>
        {
            ["build_property.AISchemaIncludeInternals"] = "true"
        };

        var result = RunGenerator(source, analyzerConfigOptions: configOptions);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var names = types.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("PublicType", names);
        Assert.Contains("InternalType", names);
    }

    [Fact]
    public void Generator_ExcludesInternals_ByDefault()
    {
        var source = """
            namespace TestNs
            {
                public class PublicType { }
                internal class InternalType { }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        var names = types.EnumerateArray()
            .Select(t => t.GetProperty("name").GetString())
            .ToList();

        Assert.Contains("PublicType", names);
        Assert.DoesNotContain("InternalType", names);
    }

    [Fact]
    public void Generator_TypesSortedDeterministically()
    {
        var source = """
            namespace Z.Ns { public class Zebra { } }
            namespace A.Ns { public class Aardvark { } }
            namespace M.Ns { public class Mango { } }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var types = doc.RootElement.GetProperty("types");
        // Filter to only our test types
        var names = types.EnumerateArray()
            .Select(t => t.GetProperty("fullName").GetString()!)
            .Where(n => n.Contains(".Ns."))
            .ToList();

        // Should be sorted by namespace then name
        var sorted = names.OrderBy(n => n).ToList();
        Assert.Equal(sorted, names);
        Assert.Equal(3, names.Count);
    }

    [Fact]
    public void Generator_SchemaVersion_Is1_0_0()
    {
        var source = """
            namespace TestNs
            {
                public class Test { }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        Assert.Equal("1.0.0", doc.RootElement.GetProperty("schemaVersion").GetString());
    }

    [Fact]
    public void Generator_HasSchemaUrl()
    {
        var source = """
            namespace TestNs
            {
                public class Test { }
            }
            """;

        var result = RunGenerator(source);
        var json = ExtractEmbeddedJson(result);

        using var doc = JsonDocument.Parse(json!);
        var schemaUrl = doc.RootElement.GetProperty("$schema").GetString();
        Assert.Contains("api-schema.json", schemaUrl);
    }

    #region Helpers

    private static GeneratorDriverRunResult RunGenerator(
        string source,
        CSharpParseOptions? parseOptions = null,
        Dictionary<string, string>? analyzerConfigOptions = null)
    {
        parseOptions ??= CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var attrTree = CSharpSyntaxTree.ParseText(ApiContractAttributeSource, parseOptions);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, attrTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new ApiSchemaGenerator();
        var optionsProvider = analyzerConfigOptions is not null
            ? new TestAnalyzerConfigOptionsProvider(analyzerConfigOptions)
            : null;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            optionsProvider: optionsProvider,
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult();
    }

    private static string? ExtractEmbeddedJson(GeneratorDriverRunResult result)
    {
        var embedderTree = result.GeneratedTrees
            .FirstOrDefault(t => Path.GetFileName(t.FilePath).Contains(".json.g.cs"));

        if (embedderTree is null) return null;

        var text = embedderTree.GetText().ToString();

        // Extract JSON from the verbatim string literal: @"..."
        var startMarker = "=> @\"";
        var startIdx = text.IndexOf(startMarker);
        if (startIdx < 0) return null;
        startIdx += startMarker.Length;

        var endIdx = text.LastIndexOf("\";");
        if (endIdx < 0 || endIdx <= startIdx) return null;

        var escaped = text[startIdx..endIdx];
        return escaped.Replace("\"\"", "\"");
    }

    /// <summary>
    /// Provides analyzer config options for testing MSBuild property passthrough.
    /// </summary>
    private sealed class TestAnalyzerConfigOptionsProvider(
        Dictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions(globalOptions);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => TestAnalyzerConfigOptions.Empty;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => TestAnalyzerConfigOptions.Empty;
    }

    private sealed class TestAnalyzerConfigOptions(Dictionary<string, string> options) : AnalyzerConfigOptions
    {
        public static TestAnalyzerConfigOptions Empty { get; } = new([]);

        public override bool TryGetValue(string key, out string value) =>
            options.TryGetValue(key, out value!);
    }

    #endregion
}
