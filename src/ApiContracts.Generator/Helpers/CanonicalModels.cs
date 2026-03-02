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
    public CanonicalDocumentation? Docs { get; set; }
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
    public CanonicalDocumentation? Docs { get; set; }
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

/// <summary>Canonical documentation.</summary>
internal sealed class CanonicalDocumentation
{
    public List<DocNode>? Summary { get; set; }
    public List<DocNode>? Remarks { get; set; }
    public List<DocNode>? Returns { get; set; }
    public List<DocNode>? Value { get; set; }
    public Dictionary<string, List<DocNode>>? Parameters { get; set; }
    public Dictionary<string, List<DocNode>>? TypeParameters { get; set; }
    public List<DocException>? Exceptions { get; set; }
    public List<DocPermission>? Permissions { get; set; }
    public List<CanonicalCodeExample>? Examples { get; set; }
    public List<string>? SeeAlso { get; set; }
}

/// <summary>
/// A single node in a rich documentation content tree.
/// Kinds: text, code, codeblock, cref, href, langword, paramref, typeparamref, para, list, note.
/// </summary>
internal sealed class DocNode
{
    /// <summary>Node kind: text, code, codeblock, cref, href, langword, paramref, typeparamref, para, list, note.</summary>
    public string Kind { get; set; } = "text";

    /// <summary>
    /// For "text", "code", and "codeblock" kinds: the text content.
    /// For "href" kind: optional display text (if different from URL).
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// For "cref": the documentation ID (e.g. "T:SampleApi.Customer").
    /// For "href": the URL.
    /// For "langword": the keyword (e.g. "null", "true", "false").
    /// For "paramref"/"typeparamref": the parameter name.
    /// For "note": the note type ("note", "tip", "caution", "warning", "important").
    /// </summary>
    public string? Value { get; set; }

    /// <summary>For "codeblock" kind: the programming language.</summary>
    public string? Language { get; set; }

    /// <summary>For "para", "note", and list items: nested child nodes.</summary>
    public List<DocNode>? Children { get; set; }

    /// <summary>For "list" kind: the list style — "bullet", "number", or "table".</summary>
    public string? Style { get; set; }

    /// <summary>For "list" kind: the list header row.</summary>
    public DocListItem? Header { get; set; }

    /// <summary>For "list" kind: the list items.</summary>
    public List<DocListItem>? Items { get; set; }

    /// <summary>Creates a text node.</summary>
    public static DocNode TextNode(string text) => new() { Kind = "text", Text = text };

    /// <summary>Creates a cref reference node.</summary>
    public static DocNode CrefNode(string cref) => new() { Kind = "cref", Value = cref };

    /// <summary>Creates an href (external link) node.</summary>
    public static DocNode HrefNode(string href, string? displayText = null) =>
        new() { Kind = "href", Value = href, Text = displayText };

    /// <summary>Creates a language keyword node.</summary>
    public static DocNode LangwordNode(string keyword) => new() { Kind = "langword", Value = keyword };

    /// <summary>Creates an inline code node (&lt;c&gt;).</summary>
    public static DocNode CodeNode(string code) => new() { Kind = "code", Text = code };

    /// <summary>Creates a code block node (&lt;code&gt;).</summary>
    public static DocNode CodeblockNode(string code, string? language = null) =>
        new() { Kind = "codeblock", Text = code, Language = language };

    /// <summary>Creates a paramref node.</summary>
    public static DocNode ParamrefNode(string name) => new() { Kind = "paramref", Value = name };

    /// <summary>Creates a typeparamref node.</summary>
    public static DocNode TypeparamrefNode(string name) => new() { Kind = "typeparamref", Value = name };

    /// <summary>Creates a paragraph node wrapping child content.</summary>
    public static DocNode ParaNode(List<DocNode> children) => new() { Kind = "para", Children = children };

    /// <summary>Creates a note/admonition node.</summary>
    public static DocNode NoteNode(string noteType, List<DocNode> children) =>
        new() { Kind = "note", Value = noteType, Children = children };

    /// <summary>Creates a list node.</summary>
    public static DocNode ListNode(string style, List<DocListItem> items, DocListItem? header = null) =>
        new() { Kind = "list", Style = style, Items = items, Header = header };
}

/// <summary>An item in a documentation list.</summary>
internal sealed class DocListItem
{
    /// <summary>Optional term for definition lists / tables.</summary>
    public List<DocNode>? Term { get; set; }

    /// <summary>The item description.</summary>
    public List<DocNode> Description { get; set; } = [];
}

/// <summary>A documented exception that a member can throw.</summary>
internal sealed class DocException
{
    /// <summary>The exception type reference (cref doc-ID).</summary>
    public string Type { get; set; } = "";

    /// <summary>Description of when the exception is thrown.</summary>
    public List<DocNode> Description { get; set; } = [];
}

/// <summary>A documented permission requirement.</summary>
internal sealed class DocPermission
{
    /// <summary>The permission type reference (cref doc-ID).</summary>
    public string Type { get; set; } = "";

    /// <summary>Description of the required permission.</summary>
    public List<DocNode> Description { get; set; } = [];
}

/// <summary>Canonical code example.</summary>
internal sealed class CanonicalCodeExample
{
    public string Language { get; set; } = "csharp";
    public string? Region { get; set; }
    public string Code { get; set; } = "";
    public List<DocNode>? Description { get; set; }
}


