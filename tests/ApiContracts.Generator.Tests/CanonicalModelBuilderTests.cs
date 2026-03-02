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
                public bool Ignore { get; set; }
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
        Assert.Equal("A documented class.", CanonicalModelBuilder.FlattenDocNodesToText(doc.Docs!.Summary));
        Assert.Equal("Some extra remarks.", CanonicalModelBuilder.FlattenDocNodesToText(doc.Docs.Remarks));
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
    public void BuildTypes_ExcludesTypesMarkedWithIgnore()
    {
        var source = """
            using ApiContracts;

            namespace TestNs
            {
                [ApiContract(Ignore = true)]
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

    [Fact]
    public void BuildTypes_CapturesStruct()
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

        var result = BuildTypesFromSource(source);

        var point = Assert.Single(result);
        Assert.Equal("Point", point.Name);
        Assert.Equal("struct", point.Kind);
        Assert.False(point.IsAbstract);
    }

    [Fact]
    public void BuildTypes_CapturesReadonlyStruct()
    {
        var source = """
            namespace TestNs
            {
                public readonly struct Money
                {
                    public decimal Amount { get; }
                    public string Currency { get; }

                    public Money(decimal amount, string currency)
                    {
                        Amount = amount;
                        Currency = currency;
                    }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var money = Assert.Single(result);
        Assert.Equal("struct", money.Kind);
        Assert.True(money.IsSealed); // readonly structs are implicitly sealed
    }

    [Fact]
    public void BuildTypes_CapturesRecord()
    {
        var source = """
            namespace TestNs
            {
                public record Person(string Name, int Age);
            }
            """;

        var result = BuildTypesFromSource(source);

        var person = Assert.Single(result);
        Assert.Equal("Person", person.Name);
        Assert.Equal("record", person.Kind);
    }

    [Fact]
    public void BuildTypes_CapturesRecordStruct()
    {
        var source = """
            namespace TestNs
            {
                public record struct Coordinate(double Lat, double Lon);
            }
            """;

        var result = BuildTypesFromSource(source);

        var coord = Assert.Single(result);
        Assert.Equal("Coordinate", coord.Name);
        Assert.Equal("record struct", coord.Kind);
    }

    [Fact]
    public void BuildTypes_CapturesStaticClass()
    {
        var source = """
            namespace TestNs
            {
                public static class MathHelper
                {
                    public static int Add(int a, int b) => a + b;
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var helper = Assert.Single(result);
        Assert.True(helper.IsStatic);

        var method = helper.Members.First(m => m.Name == "Add");
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void BuildTypes_CapturesEvents()
    {
        var source = """
            namespace TestNs
            {
                public class Publisher
                {
                    public event System.EventHandler? ItemAdded;
                    public event System.EventHandler<string>? ItemRemoved;
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var publisher = Assert.Single(result);
        var events = publisher.Members.Where(m => m.Kind == "event").ToList();
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Name == "ItemAdded");
        Assert.Contains(events, e => e.Name == "ItemRemoved");
    }

    [Fact]
    public void BuildTypes_CapturesIndexer()
    {
        var source = """
            namespace TestNs
            {
                public class Collection
                {
                    public string this[int index] => "";
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var collection = Assert.Single(result);
        var indexer = collection.Members.First(m => m.Kind == "indexer");
        Assert.Equal("this[]", indexer.Name);
        Assert.Equal("string", indexer.ReturnType);
        Assert.NotNull(indexer.Parameters);
        Assert.Single(indexer.Parameters!);
        Assert.Equal("index", indexer.Parameters![0].Name);
        Assert.Equal("int", indexer.Parameters[0].Type);
    }

    [Fact]
    public void BuildTypes_CapturesConstructor()
    {
        var source = """
            namespace TestNs
            {
                public class Service
                {
                    public Service(string name, int timeout) { }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var service = Assert.Single(result);
        var ctor = service.Members.First(m => m.Kind == "constructor");
        Assert.Equal(".ctor", ctor.Name);
        Assert.NotNull(ctor.Parameters);
        Assert.Equal(2, ctor.Parameters!.Count);
        Assert.Equal("name", ctor.Parameters[0].Name);
        Assert.Equal("timeout", ctor.Parameters[1].Name);
    }

    [Fact]
    public void BuildTypes_CapturesAsyncMethod()
    {
        var source = """
            namespace TestNs
            {
                public class DataService
                {
                    public async System.Threading.Tasks.Task<string> FetchAsync(int id) => "";
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var service = Assert.Single(result);
        var method = service.Members.First(m => m.Name == "FetchAsync");
        Assert.True(method.IsAsync);
        Assert.Equal("method", method.Kind);
    }

    [Fact]
    public void BuildTypes_CapturesOptionalParameters()
    {
        var source = """
            namespace TestNs
            {
                public class Config
                {
                    public void Setup(string name, int retries = 3, bool verbose = false) { }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var config = Assert.Single(result);
        var method = config.Members.First(m => m.Name == "Setup");
        Assert.NotNull(method.Parameters);
        Assert.Equal(3, method.Parameters!.Count);

        Assert.False(method.Parameters[0].IsOptional);

        Assert.True(method.Parameters[1].IsOptional);
        Assert.Equal("3", method.Parameters[1].DefaultValue);

        Assert.True(method.Parameters[2].IsOptional);
        Assert.Equal("False", method.Parameters[2].DefaultValue);
    }

    [Fact]
    public void BuildTypes_CapturesRefOutInParameters()
    {
        var source = """
            namespace TestNs
            {
                public class Parser
                {
                    public bool TryParse(string input, out int result)
                    {
                        result = 0;
                        return false;
                    }

                    public void Swap(ref int a, ref int b) { }

                    public int Sum(in int x, in int y) => x + y;
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var parser = Assert.Single(result);

        var tryParse = parser.Members.First(m => m.Name == "TryParse");
        Assert.Equal("out", tryParse.Parameters![1].Modifier);

        var swap = parser.Members.First(m => m.Name == "Swap");
        Assert.Equal("ref", swap.Parameters![0].Modifier);
        Assert.Equal("ref", swap.Parameters[1].Modifier);

        var sum = parser.Members.First(m => m.Name == "Sum");
        Assert.Equal("in", sum.Parameters![0].Modifier);
    }

    [Fact]
    public void BuildTypes_CapturesParamsParameter()
    {
        var source = """
            namespace TestNs
            {
                public class Formatter
                {
                    public string Format(string template, params object[] args) => "";
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var formatter = Assert.Single(result);
        var method = formatter.Members.First(m => m.Name == "Format");
        Assert.Equal("params", method.Parameters![1].Modifier);
    }

    [Fact]
    public void BuildTypes_CapturesMultipleInterfaces()
    {
        var source = """
            namespace TestNs
            {
                public class MultiImpl : System.IDisposable, System.IComparable<MultiImpl>
                {
                    public void Dispose() { }
                    public int CompareTo(MultiImpl? other) => 0;
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var multi = Assert.Single(result);
        Assert.True(multi.Interfaces.Count >= 2);
        Assert.Contains(multi.Interfaces, i => i.Contains("IDisposable"));
        Assert.Contains(multi.Interfaces, i => i.Contains("IComparable"));
    }

    [Fact]
    public void BuildTypes_CapturesObsoleteAttribute()
    {
        var source = """
            namespace TestNs
            {
                [System.Obsolete]
                public class LegacyService
                {
                    public void Run() { }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var legacy = Assert.Single(result);
        Assert.NotNull(legacy.Attributes);
        Assert.Contains(legacy.Attributes!, a => a.Name == "ObsoleteAttribute");
    }

    [Fact]
    public void BuildTypes_CapturesFlagsAttribute()
    {
        var source = """
            namespace TestNs
            {
                [System.Flags]
                public enum Permissions
                {
                    None = 0,
                    Read = 1,
                    Write = 2,
                    Execute = 4,
                    All = Read | Write | Execute
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var perms = Assert.Single(result);
        Assert.Equal("enum", perms.Kind);
        Assert.NotNull(perms.Attributes);
        Assert.Contains(perms.Attributes!, a => a.Name == "FlagsAttribute");
        Assert.NotNull(perms.EnumMembers);
        Assert.Equal(5, perms.EnumMembers!.Count);
    }

    [Fact]
    public void BuildTypes_CapturesVirtualAndOverrideMethods()
    {
        var source = """
            namespace TestNs
            {
                public class Base
                {
                    public virtual void Execute() { }
                }

                public class Derived : Base
                {
                    public override void Execute() { }
                }
            }
            """;

        var result = BuildTypesFromSource(source);

        var baseType = result.First(t => t.Name == "Base");
        var baseMethod = baseType.Members.First(m => m.Name == "Execute");
        Assert.True(baseMethod.IsVirtual);
        Assert.False(baseMethod.IsOverride);

        var derivedType = result.First(t => t.Name == "Derived");
        var derivedMethod = derivedType.Members.First(m => m.Name == "Execute");
        Assert.True(derivedMethod.IsOverride);
    }

    [Fact]
    public void BuildTypes_CapturesBaseType()
    {
        var source = """
            namespace TestNs
            {
                public class Animal { }
                public class Dog : Animal { }
            }
            """;

        var result = BuildTypesFromSource(source);

        var dog = result.First(t => t.Name == "Dog");
        Assert.Equal("TestNs.Animal", dog.BaseType);

        var animal = result.First(t => t.Name == "Animal");
        Assert.Null(animal.BaseType); // Object is not emitted
    }

    [Fact]
    public void BuildTypes_CapturesMethodWithReturnDoc()
    {
        var source = """
            namespace TestNs
            {
                public class Calculator
                {
                    /// <summary>Adds two numbers.</summary>
                    /// <param name="a">First number.</param>
                    /// <param name="b">Second number.</param>
                    /// <returns>The sum.</returns>
                    public int Add(int a, int b) => a + b;
                }
            }
            """;

        var result = BuildTypesFromSource(source,
            parseOptions: new CSharpParseOptions(documentationMode: DocumentationMode.Parse));

        var calc = Assert.Single(result);
        var method = calc.Members.First(m => m.Name == "Add");
        Assert.NotNull(method.Docs);
        Assert.Equal("Adds two numbers.", CanonicalModelBuilder.FlattenDocNodesToText(method.Docs!.Summary));
        Assert.Equal("The sum.", CanonicalModelBuilder.FlattenDocNodesToText(method.Docs.Returns));
        Assert.NotNull(method.Docs.Parameters);
        Assert.Equal("First number.", CanonicalModelBuilder.FlattenDocNodesToText(method.Docs.Parameters!["a"]));
        Assert.Equal("Second number.", CanonicalModelBuilder.FlattenDocNodesToText(method.Docs.Parameters["b"]));
    }

    [Fact]
    public void BuildTypes_CapturesDelegate()
    {
        var source = """
            namespace TestNs
            {
                public delegate void Callback(string message, int code);
            }
            """;

        var result = BuildTypesFromSource(source);

        var callback = Assert.Single(result);
        Assert.Equal("Callback", callback.Name);
        Assert.Equal("delegate", callback.Kind);
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
        return [.. compilation.Assembly.GlobalNamespace
            .GetNamespaceMembers()
            .Where(ns => ns.Name != "ApiContracts")
            .SelectMany(GetAllTypes)
            .Where(t => t.DeclaredAccessibility == Accessibility.Public)];
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
