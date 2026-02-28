// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Models;

/// <summary>
/// AI metadata from <see cref="AIContractAttribute"/> overrides.
/// </summary>
public sealed class AIMetadata
{
    /// <summary>Override display name.</summary>
    public string? Name { get; set; }

    /// <summary>AI-oriented description.</summary>
    public string? Description { get; set; }

    /// <summary>Category for grouping.</summary>
    public string? Category { get; set; }

    /// <summary>Semantic role.</summary>
    public string? Role { get; set; }

    /// <summary>Tags for search and filtering.</summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Normalized XML documentation model.
/// </summary>
public sealed class DocumentationModel
{
    /// <summary>Summary text.</summary>
    public string? Summary { get; set; }

    /// <summary>Remarks text.</summary>
    public string? Remarks { get; set; }

    /// <summary>Return value description.</summary>
    public string? Returns { get; set; }

    /// <summary>Parameter descriptions keyed by parameter name.</summary>
    public Dictionary<string, string>? Parameters { get; set; }

    /// <summary>Code examples.</summary>
    public List<CodeExample>? Examples { get; set; }

    /// <summary>See also references.</summary>
    public List<string>? SeeAlso { get; set; }
}

/// <summary>
/// A code example extracted from XML docs.
/// </summary>
public sealed class CodeExample
{
    /// <summary>Programming language.</summary>
    public string Language { get; set; } = "csharp";

    /// <summary>Optional named region.</summary>
    public string? Region { get; set; }

    /// <summary>The code content.</summary>
    public string Code { get; set; } = "";

    /// <summary>Optional description of what the example demonstrates.</summary>
    public string? Description { get; set; }
}
