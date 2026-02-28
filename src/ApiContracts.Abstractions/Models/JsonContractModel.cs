// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Models;

/// <summary>
/// JSON serialization contract for a type, modeling System.Text.Json behavior.
/// </summary>
public sealed class JsonContractModel
{
    /// <summary>The JSON contract type (object, array, value, dictionary).</summary>
    public string ContractType { get; set; } = "object";

    /// <summary>JSON properties for this type.</summary>
    public List<JsonPropertyModel> Properties { get; set; } = [];

    /// <summary>Whether the type uses camelCase naming by default.</summary>
    public bool UseCamelCase { get; set; } = true;
}

/// <summary>
/// JSON property metadata modeling System.Text.Json serialization behavior.
/// </summary>
public sealed class JsonPropertyModel
{
    /// <summary>CLR property name.</summary>
    public string ClrName { get; set; } = "";

    /// <summary>JSON property name (respecting <c>[JsonPropertyName]</c>).</summary>
    public string JsonName { get; set; } = "";

    /// <summary>JSON type.</summary>
    public string JsonType { get; set; } = "string";

    /// <summary>Whether the property is ignored during serialization.</summary>
    public bool Ignored { get; set; }

    /// <summary>Whether the property value can be null.</summary>
    public bool Nullable { get; set; }

    /// <summary>Whether the property is required.</summary>
    public bool Required { get; set; }

    /// <summary>CLR type name.</summary>
    public string ClrType { get; set; } = "";

    /// <summary>Description from XML docs or AI metadata.</summary>
    public string? Description { get; set; }
}
