using Xunit;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator.Tests;

public class SchemaEmitterTests
{
    [Fact]
    public void EmitAssemblySchema_ProducesValidStructure()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "TestType",
                FullName = "TestNs.TestType",
                Namespace = "TestNs",
                Kind = "class",
                Accessibility = "public",
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"schemaVersion\": \"1.0.0\"", json);
        Assert.Contains("\"rootSchema\": \"../../schema.json\"", json);
        Assert.Contains("\"name\": \"TestAssembly\"", json);
        Assert.Contains("\"version\": \"1.0.0\"", json);
        Assert.Contains("\"targetFramework\": \"net10.0\"", json);
        Assert.Contains("\"apiHash\": \"sha256:abc\"", json);
        Assert.Contains("\"name\": \"TestType\"", json);
        Assert.Contains("\"fullName\": \"TestNs.TestType\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_IncludesEnumMembers()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Status",
                FullName = "TestNs.Status",
                Namespace = "TestNs",
                Kind = "enum",
                EnumMembers =
                [
                    new CanonicalEnumMember { Name = "Active", Value = 0, Description = "Active status" },
                    new CanonicalEnumMember { Name = "Inactive", Value = 1 },
                ]
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"enumMembers\":", json);
        Assert.Contains("\"name\": \"Active\"", json);
        Assert.Contains("\"value\": 0", json);
        Assert.Contains("\"description\": \"Active status\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_IncludesJsonContract()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Dto",
                FullName = "TestNs.Dto",
                Namespace = "TestNs",
                Kind = "class",
                Json = new CanonicalJsonContract
                {
                    ContractType = "object",
                    UseCamelCase = true,
                    Properties =
                    [
                        new CanonicalJsonProperty
                        {
                            ClrName = "Name",
                            JsonName = "name",
                            JsonType = "string",
                            ClrType = "string",
                            Required = true,
                        },
                        new CanonicalJsonProperty
                        {
                            ClrName = "Age",
                            JsonName = "age",
                            JsonType = "number",
                            ClrType = "int",
                            Nullable = true,
                        }
                    ]
                }
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"contractType\": \"object\"", json);
        Assert.Contains("\"useCamelCase\": true", json);
        Assert.Contains("\"jsonName\": \"name\"", json);
        Assert.Contains("\"required\": true", json);
        Assert.Contains("\"nullable\": true", json);
    }

    [Fact]
    public void EmitAssemblySchema_IncludesAIMetadata()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Svc",
                FullName = "TestNs.Svc",
                Namespace = "TestNs",
                Kind = "interface",
                AI = new CanonicalAIMetadata
                {
                    Name = "TestService",
                    Description = "A test service",
                    Category = "Services",
                    Role = "service",
                    Tags = ["api", "test"],
                }
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"ai\":", json);
        Assert.Contains("\"name\": \"TestService\"", json);
        Assert.Contains("\"description\": \"A test service\"", json);
        Assert.Contains("\"category\": \"Services\"", json);
        Assert.Contains("\"role\": \"service\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_SortsTypesByNamespaceThenName()
    {
        var types = new List<CanonicalType>
        {
            new() { Name = "Z", FullName = "B.Z", Namespace = "B", Kind = "class" },
            new() { Name = "A", FullName = "A.A", Namespace = "A", Kind = "class" },
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        var aIndex = json.IndexOf("\"fullName\": \"A.A\"");
        var zIndex = json.IndexOf("\"fullName\": \"B.Z\"");

        Assert.True(aIndex < zIndex);
    }
}
