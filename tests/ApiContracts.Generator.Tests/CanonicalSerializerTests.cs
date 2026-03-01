using Xunit;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator.Tests;

public class CanonicalSerializerTests
{
    [Fact]
    public void SerializeForHashing_EmptyList_ReturnsEmptyArray()
    {
        var result = CanonicalSerializer.SerializeForHashing([]);
        Assert.Equal("[]", result);
    }

    [Fact]
    public void SerializeForHashing_SingleType_ProducesValidJson()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "TestClass",
                FullName = "TestNamespace.TestClass",
                Namespace = "TestNamespace",
                Kind = "class",
                Accessibility = "public",
            }
        };

        var result = CanonicalSerializer.SerializeForHashing(types);

        Assert.StartsWith("[{", result);
        Assert.EndsWith("}]", result);
        Assert.Contains("\"name\":\"TestClass\"", result);
        Assert.Contains("\"fullName\":\"TestNamespace.TestClass\"", result);
        Assert.Contains("\"namespace\":\"TestNamespace\"", result);
        Assert.Contains("\"kind\":\"class\"", result);
    }

    [Fact]
    public void SerializeForHashing_SortsByNamespaceThenName()
    {
        var types = new List<CanonicalType>
        {
            new() { Name = "Zebra", FullName = "B.Zebra", Namespace = "B", Kind = "class" },
            new() { Name = "Alpha", FullName = "A.Alpha", Namespace = "A", Kind = "class" },
            new() { Name = "Beta", FullName = "A.Beta", Namespace = "A", Kind = "class" },
        };

        var result = CanonicalSerializer.SerializeForHashing(types);
        var alphaIndex = result.IndexOf("\"A.Alpha\"");
        var betaIndex = result.IndexOf("\"A.Beta\"");
        var zebraIndex = result.IndexOf("\"B.Zebra\"");

        Assert.True(alphaIndex < betaIndex);
        Assert.True(betaIndex < zebraIndex);
    }

    [Fact]
    public void SerializeForHashing_IsDeterministic()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "Order",
                FullName = "Domain.Order",
                Namespace = "Domain",
                Kind = "class",
                Members =
                [
                    new CanonicalMember
                    {
                        Name = "Id",
                        Kind = "property",
                        Signature = "Guid Id { get; set; }",
                        ReturnType = "System.Guid",
                    },
                    new CanonicalMember
                    {
                        Name = "Total",
                        Kind = "property",
                        Signature = "decimal Total { get; }",
                        ReturnType = "decimal",
                    }
                ]
            }
        };

        var result1 = CanonicalSerializer.SerializeForHashing(types);
        var result2 = CanonicalSerializer.SerializeForHashing(types);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void SerializeForHashing_ExcludesCodeSampleContent()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "X",
                FullName = "T.X",
                Namespace = "T",
                Kind = "class",
                Docs = new CanonicalDocumentation
                {
                    Summary = "A summary",
                    Examples =
                    [
                        new CanonicalCodeExample
                        {
                            Language = "csharp",
                            Code = "var x = new X();",
                        }
                    ]
                }
            }
        };

        var result = CanonicalSerializer.SerializeForHashing(types);

        // Summary is included in hash
        Assert.Contains("\"summary\":\"A summary\"", result);
        // Code sample content is excluded from hash
        Assert.DoesNotContain("var x = new X()", result);
    }

    [Fact]
    public void SerializeForHashing_SortsMembersByKindThenName()
    {
        var types = new List<CanonicalType>
        {
            new()
            {
                Name = "C",
                FullName = "N.C",
                Namespace = "N",
                Kind = "class",
                Members =
                [
                    new CanonicalMember { Name = "Zebra", Kind = "property", Signature = "int Zebra" },
                    new CanonicalMember { Name = "Alpha", Kind = "property", Signature = "int Alpha" },
                    new CanonicalMember { Name = "DoWork", Kind = "method", Signature = "void DoWork()" },
                    new CanonicalMember { Name = ".ctor", Kind = "constructor", Signature = "C()" },
                ]
            }
        };

        var result = CanonicalSerializer.SerializeForHashing(types);

        var ctorIndex = result.IndexOf("\"constructor\"");
        var methodIndex = result.IndexOf("\"method\"");
        var propIndex = result.IndexOf("\"property\"");

        Assert.True(ctorIndex < methodIndex);
        Assert.True(methodIndex < propIndex);
    }
}
