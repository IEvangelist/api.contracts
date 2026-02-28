// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using ApiContracts.Generator.Helpers;

namespace ApiContracts.Generator.Tests;

public class CanonicalModelTests
{
    [Theory]
    [InlineData("public class MyClass { }", "MyClass", "class")]
    [InlineData("public interface IMyInterface { }", "IMyInterface", "interface")]
    [InlineData("public enum MyEnum { A, B }", "MyEnum", "enum")]
    [InlineData("public struct MyStruct { }", "MyStruct", "struct")]
    [InlineData("public record MyRecord;", "MyRecord", "record")]
    [InlineData("public record struct MyRecordStruct;", "MyRecordStruct", "record struct")]
    [InlineData("public delegate void MyDelegate();", "MyDelegate", "delegate")]
    public void CanonicalType_GetTypeKind_ReturnsCorrectKind(string source, string typeName, string expectedKind)
    {
        var compilation = CreateCompilation($"namespace TestNs {{ {source} }}");
        var symbol = compilation.GetTypeByMetadataName($"TestNs.{typeName}")!;

        var result = CanonicalType.GetTypeKind(symbol);

        Assert.Equal(expectedKind, result);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Fact]
    public void CanonicalType_DefaultValues_AreCorrect()
    {
        var type = new CanonicalType();

        Assert.Equal("", type.Name);
        Assert.Equal("", type.FullName);
        Assert.Equal("", type.Namespace);
        Assert.Equal("", type.Kind);
        Assert.Equal("public", type.Accessibility);
        Assert.False(type.IsAbstract);
        Assert.False(type.IsSealed);
        Assert.False(type.IsStatic);
        Assert.False(type.IsGeneric);
        Assert.Null(type.GenericParameters);
        Assert.Null(type.BaseType);
        Assert.Empty(type.Interfaces);
        Assert.Empty(type.Members);
        Assert.Null(type.AI);
        Assert.Null(type.Docs);
        Assert.Null(type.Json);
        Assert.Null(type.EnumMembers);
        Assert.Null(type.Attributes);
    }

    [Fact]
    public void CanonicalMember_DefaultValues_AreCorrect()
    {
        var member = new CanonicalMember();

        Assert.Equal("", member.Name);
        Assert.Equal("", member.Kind);
        Assert.Equal("public", member.Accessibility);
        Assert.False(member.IsStatic);
        Assert.False(member.IsAbstract);
        Assert.False(member.IsVirtual);
        Assert.False(member.IsOverride);
        Assert.False(member.IsAsync);
        Assert.Null(member.ReturnType);
        Assert.False(member.IsReturnNullable);
        Assert.Null(member.Parameters);
        Assert.Null(member.GenericParameters);
        Assert.Equal("", member.Signature);
    }

    [Fact]
    public void CanonicalJsonProperty_DefaultValues_AreCorrect()
    {
        var prop = new CanonicalJsonProperty();

        Assert.Equal("", prop.ClrName);
        Assert.Equal("", prop.JsonName);
        Assert.Equal("string", prop.JsonType);
        Assert.False(prop.Ignored);
        Assert.False(prop.Nullable);
        Assert.False(prop.Required);
        Assert.Equal("", prop.ClrType);
        Assert.Null(prop.Description);
    }

    [Fact]
    public void CanonicalParameter_DefaultValues_AreCorrect()
    {
        var param = new CanonicalParameter();

        Assert.Equal("", param.Name);
        Assert.Equal("", param.Type);
        Assert.False(param.IsNullable);
        Assert.False(param.IsOptional);
        Assert.Null(param.DefaultValue);
        Assert.Null(param.Modifier);
    }

    [Fact]
    public void CanonicalAIMetadata_DefaultValues_AreAllNull()
    {
        var ai = new CanonicalAIMetadata();

        Assert.Null(ai.Name);
        Assert.Null(ai.Description);
        Assert.Null(ai.Category);
        Assert.Null(ai.Role);
        Assert.Null(ai.Tags);
    }

    [Fact]
    public void CanonicalDocumentation_DefaultValues_AreAllNull()
    {
        var docs = new CanonicalDocumentation();

        Assert.Null(docs.Summary);
        Assert.Null(docs.Remarks);
        Assert.Null(docs.Returns);
        Assert.Null(docs.Parameters);
        Assert.Null(docs.Examples);
        Assert.Null(docs.SeeAlso);
    }

    [Fact]
    public void CanonicalJsonContract_DefaultValues_AreCorrect()
    {
        var contract = new CanonicalJsonContract();

        Assert.Equal("object", contract.ContractType);
        Assert.Empty(contract.Properties);
        Assert.False(contract.UseCamelCase);
    }
}
