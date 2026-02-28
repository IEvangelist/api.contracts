// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Generator.Helpers;

/// <summary>Canonical representation of a type for schema emission.</summary>
internal sealed class CanonicalType
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Accessibility { get; set; } = "public";
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsStatic { get; set; }
    public bool IsGeneric { get; set; }
    public List<CanonicalGenericParameter>? GenericParameters { get; set; }
    public string? BaseType { get; set; }
    public List<string> Interfaces { get; set; } = [];
    public List<CanonicalMember> Members { get; set; } = [];
    public CanonicalAIMetadata? AI { get; set; }
    public CanonicalDocumentation? Docs { get; set; }
    public CanonicalJsonContract? Json { get; set; }
    public List<CanonicalEnumMember>? EnumMembers { get; set; }
    public List<CanonicalAttribute>? Attributes { get; set; }

    internal static string GetTypeKind(Microsoft.CodeAnalysis.INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord) return symbol.IsValueType ? "record struct" : "record";
        return symbol.TypeKind switch
        {
            Microsoft.CodeAnalysis.TypeKind.Class => "class",
            Microsoft.CodeAnalysis.TypeKind.Struct => "struct",
            Microsoft.CodeAnalysis.TypeKind.Interface => "interface",
            Microsoft.CodeAnalysis.TypeKind.Enum => "enum",
            Microsoft.CodeAnalysis.TypeKind.Delegate => "delegate",
            _ => "class",
        };
    }
}

/// <summary>Canonical generic parameter.</summary>
internal sealed class CanonicalGenericParameter
{
    public string Name { get; set; } = "";
    public List<string> Constraints { get; set; } = [];
}

/// <summary>Canonical member.</summary>
internal sealed class CanonicalMember
{
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Accessibility { get; set; } = "public";
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsOverride { get; set; }
    public bool IsAsync { get; set; }
    public string? ReturnType { get; set; }
    public bool IsReturnNullable { get; set; }
    public List<CanonicalParameter>? Parameters { get; set; }
    public List<CanonicalGenericParameter>? GenericParameters { get; set; }
    public string Signature { get; set; } = "";
    public CanonicalAIMetadata? AI { get; set; }
    public CanonicalDocumentation? Docs { get; set; }
    public CanonicalJsonProperty? Json { get; set; }
    public List<CanonicalAttribute>? Attributes { get; set; }
}

/// <summary>Canonical parameter.</summary>
internal sealed class CanonicalParameter
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsNullable { get; set; }
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
    public string? Modifier { get; set; }
}

/// <summary>Canonical enum member.</summary>
internal sealed class CanonicalEnumMember
{
    public string Name { get; set; } = "";
    public long Value { get; set; }
    public string? Description { get; set; }
}

/// <summary>Canonical attribute.</summary>
internal sealed class CanonicalAttribute
{
    public string Name { get; set; } = "";
    public Dictionary<string, string>? Arguments { get; set; }
}

/// <summary>Canonical AI metadata.</summary>
internal sealed class CanonicalAIMetadata
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Role { get; set; }
    public List<string>? Tags { get; set; }
}

/// <summary>Canonical documentation.</summary>
internal sealed class CanonicalDocumentation
{
    public string? Summary { get; set; }
    public string? Remarks { get; set; }
    public string? Returns { get; set; }
    public Dictionary<string, string>? Parameters { get; set; }
    public List<CanonicalCodeExample>? Examples { get; set; }
    public List<string>? SeeAlso { get; set; }
}

/// <summary>Canonical code example.</summary>
internal sealed class CanonicalCodeExample
{
    public string Language { get; set; } = "csharp";
    public string? Region { get; set; }
    public string Code { get; set; } = "";
    public string? Description { get; set; }
}

/// <summary>Canonical JSON contract.</summary>
internal sealed class CanonicalJsonContract
{
    public string ContractType { get; set; } = "object";
    public List<CanonicalJsonProperty> Properties { get; set; } = [];
    public bool UseCamelCase { get; set; }
}

/// <summary>Canonical JSON property.</summary>
internal sealed class CanonicalJsonProperty
{
    public string ClrName { get; set; } = "";
    public string JsonName { get; set; } = "";
    public string JsonType { get; set; } = "string";
    public bool Ignored { get; set; }
    public bool Nullable { get; set; }
    public bool Required { get; set; }
    public string ClrType { get; set; } = "";
    public string? Description { get; set; }
}
