// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator.Tests;

public class CanonicalModelBuilderTests
{
    private const string ApiContractAttributeSource = """
        namespace ApiContracts
        {
            [System.AttributeUsage(
                System.AttributeTargets.Class | System.AttributeTargets.Struct |
                System.AttributeTargets.Interface | System.AttributeTargets.Enum |
                System.AttributeTargets.Method | System.AttributeTargets.Property |
                System.AttributeTargets.Field | System.AttributeTargets.Parameter |
                System.AttributeTargets.Event,
                AllowMultiple = false, Inherited = true)]
            public sealed class ApiContractAttribute : System.Attribute
            {
                public string? Name { get; set; }
                public string? Description { get; set; }
                public string? Category { get; set; }
                public string? Role { get; set; }
                public string? Tags { get; set; }
                public bool Exclude { get; set; }
            }
        }
        """;

    [Fact]
    public void BuildTypes_CapturesClassProperties()
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

        var result = BuildTypesFromSource(source);

        var person = Assert.Single(result);
        Assert.Equal("Person", person.Name);
        Assert.Equal("TestNs.Person", person.FullName);
        Assert.Equal("TestNs", person.Namespace);
        Assert.Equal("class", person.Kind);

        var nameProp = person.Members.First(m => m.Name == "Name");
        Assert.Equal("property", nameProp.Kind);
        Assert.Equal("string", nameProp.ReturnType);

        var ageProp = person.Members.First(m => m.Name == "Age");
        Assert.Equal("property", ageProp.Kind);
        Assert.Equal("int", ageProp.ReturnType);
    }

    [Fact]
    public void BuildTypes_CapturesInterfaceMethods()
    {
        var source = """
            namespace TestNs
            {
                public interface IGreeter
                {
                    string Greet(string name, int times);
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var greeter = Assert.Single(result);
        Assert.Equal("IGreeter", greeter.Name);
        Assert.Equal("interface", greeter.Kind);

        var method = Assert.Single(greeter.Members);
        Assert.Equal("Greet", method.Name);
        Assert.Equal("method", method.Kind);
        Assert.Equal("string", method.ReturnType);
        Assert.NotNull(method.Parameters);
        Assert.Equal(2, method.Parameters!.Count);
        Assert.Equal("name", method.Parameters[0].Name);
        Assert.Equal("string", method.Parameters[0].Type);
        Assert.Equal("times", method.Parameters[1].Name);
        Assert.Equal("int", method.Parameters[1].Type);
    }

    [Fact]
    public void BuildTypes_CapturesEnumMembers()
    {
        var source = """
            namespace TestNs
            {
                public enum Color
                {
                    Red = 0,
                    Green = 1,
                    Blue = 2
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var color = Assert.Single(result);
        Assert.Equal("Color", color.Name);
        Assert.Equal("enum", color.Kind);
        Assert.NotNull(color.EnumMembers);
        Assert.Equal(3, color.EnumMembers!.Count);
        Assert.Equal("Red", color.EnumMembers[0].Name);
        Assert.Equal(0, color.EnumMembers[0].Value);
        Assert.Equal("Green", color.EnumMembers[1].Name);
        Assert.Equal(1, color.EnumMembers[1].Value);
        Assert.Equal("Blue", color.EnumMembers[2].Name);
        Assert.Equal(2, color.EnumMembers[2].Value);
    }

    [Fact]
    public void BuildTypes_CapturesXmlDocumentation()
    {
        var source = """
            namespace TestNs
            {
                /// <summary>A documented class.</summary>
                /// <remarks>Some extra remarks.</remarks>
                public class Documented
                {
                    public int Id { get; set; }
                }
            }
            """;

        var result = BuildTypesFromSource(source, parseOptions: new CSharpParseOptions(documentationMode: DocumentationMode.Parse));

        var doc = Assert.Single(result);
        Assert.NotNull(doc.Docs);
        Assert.Equal("A documented class.", doc.Docs!.Summary);
        Assert.Equal("Some extra remarks.", doc.Docs.Remarks);
    }

    [Fact]
    public void BuildTypes_CapturesAIMetadata()
    {
        var source = """
            using ApiContracts;

            namespace TestNs
            {
                [ApiContract(Name = "TestService", Description = "A test service", Category = "Services", Role = "service", Tags = "api,test")]
                public class MyService
                {
                    public int Id { get; set; }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var svc = Assert.Single(result);
        Assert.NotNull(svc.AI);
        Assert.Equal("TestService", svc.AI!.Name);
        Assert.Equal("A test service", svc.AI.Description);
        Assert.Equal("Services", svc.AI.Category);
        Assert.Equal("service", svc.AI.Role);
        Assert.NotNull(svc.AI.Tags);
        Assert.Contains("api", svc.AI.Tags!);
        Assert.Contains("test", svc.AI.Tags!);
    }

    [Fact]
    public void BuildTypes_CapturesGenericTypes()
    {
        var source = """
            namespace TestNs
            {
                public class Repository<T> where T : class, new()
                {
                    public T? Find(int id) => default;
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var repo = Assert.Single(result);
        Assert.True(repo.IsGeneric);
        Assert.NotNull(repo.GenericParameters);
        Assert.Single(repo.GenericParameters!);
        Assert.Equal("T", repo.GenericParameters![0].Name);
        Assert.Contains("class", repo.GenericParameters[0].Constraints);
        Assert.Contains("new()", repo.GenericParameters[0].Constraints);
    }

    [Fact]
    public void BuildTypes_ExcludesTypesMarkedWithExclude()
    {
        var source = """
            using ApiContracts;

            namespace TestNs
            {
                [ApiContract(Exclude = true)]
                public class Hidden { }

                public class Visible
                {
                    public int Id { get; set; }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        Assert.Single(result);
        Assert.Equal("Visible", result[0].Name);
    }

    [Fact]
    public void BuildTypes_CapturesAbstractAndSealedModifiers()
    {
        var source = """
            namespace TestNs
            {
                public abstract class AbstractBase
                {
                    public abstract void Execute();
                }

                public sealed class SealedChild : AbstractBase
                {
                    public override void Execute() { }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var abstractType = result.First(t => t.Name == "AbstractBase");
        Assert.True(abstractType.IsAbstract);
        Assert.False(abstractType.IsSealed);

        var sealedType = result.First(t => t.Name == "SealedChild");
        Assert.False(sealedType.IsAbstract);
        Assert.True(sealedType.IsSealed);
    }

    [Fact]
    public void BuildTypes_CapturesJsonContract()
    {
        var source = """
            namespace TestNs
            {
                public class UserDto
                {
                    public string FirstName { get; set; }
                    public int Age { get; set; }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var dto = Assert.Single(result);
        Assert.NotNull(dto.Json);
        Assert.Equal("object", dto.Json!.ContractType);
        Assert.True(dto.Json.UseCamelCase);

        var firstNameProp = dto.Json.Properties.First(p => p.ClrName == "FirstName");
        Assert.Equal("firstName", firstNameProp.JsonName);
        Assert.Equal("string", firstNameProp.JsonType);

        var ageProp = dto.Json.Properties.First(p => p.ClrName == "Age");
        Assert.Equal("age", ageProp.JsonName);
        Assert.Equal("number", ageProp.JsonType);
    }

    [Fact]
    public void BuildTypes_CapturesNullableReturnTypes()
    {
        var source = """
            namespace TestNs
            {
                public class Settings
                {
                    public string? NullableProp { get; set; }
                    public string NonNullProp { get; set; } = "";
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var settings = Assert.Single(result);
        var nullable = settings.Members.First(m => m.Name == "NullableProp");
        Assert.True(nullable.IsReturnNullable);

        var nonNull = settings.Members.First(m => m.Name == "NonNullProp");
        Assert.False(nonNull.IsReturnNullable);
    }

    private static List<CanonicalType> BuildTypesFromSource(string source, CSharpParseOptions? parseOptions = null)
    {
        var compilation = CreateCompilation(source, parseOptions);
        var types = GetPublicTypes(compilation);
        var builder = new CanonicalModelBuilder(compilation);
        return builder.BuildTypes(types);
    }

    private static ImmutableArray<INamedTypeSymbol> GetPublicTypes(CSharpCompilation compilation)
    {
        return compilation.Assembly.GlobalNamespace
            .GetNamespaceMembers()
            .Where(ns => ns.Name != "ApiContracts")
            .SelectMany(GetAllTypes)
            .Where(t => t.DeclaredAccessibility == Accessibility.Public)
            .ToImmutableArray();
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var type in GetAllTypes(child))
            {
                yield return type;
            }
        }
    }

    private static CSharpCompilation CreateCompilation(string source, CSharpParseOptions? parseOptions = null)
    {
        parseOptions ??= CSharpParseOptions.Default;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var attrTree = CSharpSyntaxTree.ParseText(ApiContractAttributeSource, parseOptions);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree, attrTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
    }
}
