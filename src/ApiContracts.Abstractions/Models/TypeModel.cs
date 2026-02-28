// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Models;

/// <summary>
/// Represents a public type in the canonical API model.
/// </summary>
public sealed class TypeModel
{
    /// <summary>Simple type name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Fully qualified name including namespace.</summary>
    public string FullName { get; set; } = "";

    /// <summary>Namespace.</summary>
    public string Namespace { get; set; } = "";

    /// <summary>Type kind: class, struct, interface, enum, record, delegate.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Accessibility: public, internal.</summary>
    public string Accessibility { get; set; } = "public";

    /// <summary>Whether the type is abstract.</summary>
    public bool IsAbstract { get; set; }

    /// <summary>Whether the type is sealed.</summary>
    public bool IsSealed { get; set; }

    /// <summary>Whether the type is static.</summary>
    public bool IsStatic { get; set; }

    /// <summary>Whether the type is generic.</summary>
    public bool IsGeneric { get; set; }

    /// <summary>Generic type parameters.</summary>
    public List<GenericParameterModel>? GenericParameters { get; set; }

    /// <summary>Base type, if any.</summary>
    public string? BaseType { get; set; }

    /// <summary>Implemented interfaces.</summary>
    public List<string> Interfaces { get; set; } = [];

    /// <summary>Members of this type.</summary>
    public List<MemberModel> Members { get; set; } = [];

    /// <summary>AI metadata overrides.</summary>
    public AIMetadata? AI { get; set; }

    /// <summary>Normalized XML documentation.</summary>
    public DocumentationModel? Docs { get; set; }

    /// <summary>JSON serialization contract info.</summary>
    public JsonContractModel? Json { get; set; }

    /// <summary>Enum members (only for enum types).</summary>
    public List<EnumMemberModel>? EnumMembers { get; set; }

    /// <summary>Type-level attributes that affect the API shape.</summary>
    public List<AttributeModel>? Attributes { get; set; }
}

/// <summary>
/// Represents a generic type parameter.
/// </summary>
public sealed class GenericParameterModel
{
    /// <summary>Parameter name (e.g., "T").</summary>
    public string Name { get; set; } = "";

    /// <summary>Constraints on this type parameter.</summary>
    public List<string> Constraints { get; set; } = [];
}

/// <summary>
/// Represents a public member (method, property, field, event, constructor).
/// </summary>
public sealed class MemberModel
{
    /// <summary>Member name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Member kind: method, property, field, event, constructor, indexer, operator.</summary>
    public string Kind { get; set; } = "";

    /// <summary>Accessibility.</summary>
    public string Accessibility { get; set; } = "public";

    /// <summary>Whether the member is static.</summary>
    public bool IsStatic { get; set; }

    /// <summary>Whether the member is abstract.</summary>
    public bool IsAbstract { get; set; }

    /// <summary>Whether the member is virtual.</summary>
    public bool IsVirtual { get; set; }

    /// <summary>Whether the member is an override.</summary>
    public bool IsOverride { get; set; }

    /// <summary>Whether the member is async.</summary>
    public bool IsAsync { get; set; }

    /// <summary>Return type (for methods and properties).</summary>
    public string? ReturnType { get; set; }

    /// <summary>Whether the return type is nullable.</summary>
    public bool IsReturnNullable { get; set; }

    /// <summary>Method or indexer parameters.</summary>
    public List<ParameterModel>? Parameters { get; set; }

    /// <summary>Generic type parameters (for generic methods).</summary>
    public List<GenericParameterModel>? GenericParameters { get; set; }

    /// <summary>Complete member signature string.</summary>
    public string Signature { get; set; } = "";

    /// <summary>AI metadata overrides.</summary>
    public AIMetadata? AI { get; set; }

    /// <summary>Normalized XML documentation.</summary>
    public DocumentationModel? Docs { get; set; }

    /// <summary>JSON serialization metadata for this member.</summary>
    public JsonPropertyModel? Json { get; set; }

    /// <summary>Member-level attributes that affect the API shape.</summary>
    public List<AttributeModel>? Attributes { get; set; }
}

/// <summary>
/// Represents a method/constructor parameter.
/// </summary>
public sealed class ParameterModel
{
    /// <summary>Parameter name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Parameter type.</summary>
    public string Type { get; set; } = "";

    /// <summary>Whether nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Whether optional (has default value).</summary>
    public bool IsOptional { get; set; }

    /// <summary>Default value, if optional.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Parameter modifier (ref, out, in, params).</summary>
    public string? Modifier { get; set; }

    /// <summary>Parameter documentation.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Represents an enum member.
/// </summary>
public sealed class EnumMemberModel
{
    /// <summary>Enum member name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Numeric value.</summary>
    public long Value { get; set; }

    /// <summary>Documentation summary.</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Represents an attribute applied to a type or member.
/// </summary>
public sealed class AttributeModel
{
    /// <summary>Attribute type name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Attribute arguments.</summary>
    public Dictionary<string, string>? Arguments { get; set; }
}
