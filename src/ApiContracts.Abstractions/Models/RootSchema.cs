// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ApiContracts.Models;

/// <summary>
/// Root schema model containing language definitions, templates, placeholders, and language list.
/// Corresponds to <c>ai-skills/apis/schema.json</c>.
/// </summary>
public sealed class RootSchema
{
    /// <summary>Schema format version.</summary>
    public string SchemaVersion { get; set; } = "1.0.0";

    /// <summary>Generator identifier.</summary>
    public string Generator { get; set; } = "ApiContracts.Generator";

    /// <summary>Language definitions for polyglot rendering.</summary>
    public LanguageDefinitions Languages { get; set; } = new();

    /// <summary>Documentation template mappings.</summary>
    public DocumentationConfig Documentation { get; set; } = new();
}

/// <summary>
/// Language definitions for polyglot code rendering.
/// </summary>
public sealed class LanguageDefinitions
{
    /// <summary>Available language entries.</summary>
    public List<LanguageEntry> Available { get; set; } = [];

    /// <summary>Default language identifier.</summary>
    public string Default { get; set; } = "csharp";
}

/// <summary>
/// A single supported language for code rendering.
/// </summary>
public sealed class LanguageEntry
{
    /// <summary>Language identifier (e.g., "csharp", "typescript").</summary>
    public string Id { get; set; } = "";

    /// <summary>Display name (e.g., "C#", "TypeScript").</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>File extension (e.g., ".cs", ".ts").</summary>
    public string Extension { get; set; } = "";

    /// <summary>Syntax highlighting alias for fenced code blocks.</summary>
    public string SyntaxAlias { get; set; } = "";
}

/// <summary>
/// Documentation template and placeholder configuration.
/// </summary>
public sealed class DocumentationConfig
{
    /// <summary>Template mappings (e.g., type → TypePage.mdx).</summary>
    public Dictionary<string, string> Templates { get; set; } = [];

    /// <summary>Placeholder definitions (e.g., typeName → "{type.name}").</summary>
    public Dictionary<string, string> Placeholders { get; set; } = [];
}
