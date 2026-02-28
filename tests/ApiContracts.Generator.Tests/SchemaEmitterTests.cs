// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        Assert.Contains("\"$schema\":", json);
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
    public void EmitAssemblySchema_DoesNotContainTrailingCommasOrSentinels()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Clean",
                FullName = "TestNs.Clean",
                Namespace = "TestNs",
                Kind = "class",
                Accessibility = "public",
                Members =
                [
                    new CanonicalMember
                    {
                        Name = "Id",
                        Kind = "property",
                        Signature = "int Clean.Id",
                        ReturnType = "int",
                    }
                ]
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.DoesNotContain("\"_\":", json);
        Assert.DoesNotContain("\"generatedAt\":", json);
        Assert.DoesNotContain("\"attributes\":", json);
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

    [Fact]
    public void EmitAssemblySchema_IncludesDocumentation()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Documented",
                FullName = "TestNs.Documented",
                Namespace = "TestNs",
                Kind = "class",
                Docs = new CanonicalDocumentation
                {
                    Summary = "A documented class.",
                    Remarks = "Some remarks.",
                    Returns = "The result.",
                    Parameters = new Dictionary<string, string>
                    {
                        ["id"] = "The identifier."
                    }
                }
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"summary\": \"A documented class.\"", json);
        Assert.Contains("\"remarks\": \"Some remarks.\"", json);
        Assert.Contains("\"returns\": \"The result.\"", json);
        Assert.Contains("\"id\": \"The identifier.\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_IncludesGenericParameters()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Repo",
                FullName = "TestNs.Repo",
                Namespace = "TestNs",
                Kind = "class",
                IsGeneric = true,
                GenericParameters =
                [
                    new CanonicalGenericParameter
                    {
                        Name = "T",
                        Constraints = ["class", "new()"]
                    }
                ]
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"isGeneric\": true", json);
        Assert.Contains("\"genericParameters\":", json);
        Assert.Contains("\"name\": \"T\"", json);
        Assert.Contains("\"constraints\":", json);
        Assert.Contains("\"class\"", json);
        Assert.Contains("\"new()\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_IncludesInterfaces()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "MyClass",
                FullName = "TestNs.MyClass",
                Namespace = "TestNs",
                Kind = "class",
                Interfaces = ["System.IDisposable", "System.IComparable"]
            }
        };

        var config = new AssemblyConfig();
        var json = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Contains("\"interfaces\":", json);
        Assert.Contains("\"System.IDisposable\"", json);
        Assert.Contains("\"System.IComparable\"", json);
    }

    [Fact]
    public void EmitAssemblySchema_IsDeterministic()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "B",
                FullName = "TestNs.B",
                Namespace = "TestNs",
                Kind = "class",
                Accessibility = "public",
            },
            new()
            {
                Name = "A",
                FullName = "TestNs.A",
                Namespace = "TestNs",
                Kind = "class",
                Accessibility = "public",
            },
        };

        var config = new AssemblyConfig();
        var json1 = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);
        var json2 = SchemaEmitter.EmitAssemblySchema(
            "TestAssembly", "1.0.0", "net10.0", types, "sha256:abc", config);

        Assert.Equal(json1, json2);
    }
}
