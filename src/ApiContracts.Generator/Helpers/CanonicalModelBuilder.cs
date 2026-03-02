// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Builds canonical API models from Roslyn symbols.
/// </summary>
internal sealed class CanonicalModelBuilder(Compilation compilation)
{
    private readonly Compilation _compilation = compilation;

    public List<CanonicalType> BuildTypes(ImmutableArray<INamedTypeSymbol> types)
    {
        var result = new List<CanonicalType>();

        foreach (var type in types.OrderBy(t => t.ToDisplayString()))
        {
            if (ShouldExclude(type))
            {
                continue;
            }

            result.Add(BuildType(type));
        }

        return result;
    }

    private CanonicalType BuildType(INamedTypeSymbol symbol)
    {
        var type = new CanonicalType
        {
            Name = symbol.Name,
            FullName = symbol.ToDisplayString(),
            Namespace = symbol.ContainingNamespace?.ToDisplayString() ?? "",
            Kind = CanonicalType.GetTypeKind(symbol),
            Accessibility = symbol.DeclaredAccessibility.ToString().ToLowerInvariant(),
            IsAbstract = symbol.IsAbstract && symbol.TypeKind != TypeKind.Interface,
            IsSealed = symbol.IsSealed,
            IsStatic = symbol.IsStatic,
            IsGeneric = symbol.IsGenericType,
        };

        // Generic parameters
        if (symbol.IsGenericType)
        {
            type.GenericParameters = [.. symbol.TypeParameters.Select(BuildGenericParameter)];
        }

        // Base type
        if (symbol.BaseType is not null &&
            symbol.BaseType.SpecialType != SpecialType.System_Object &&
            symbol.BaseType.SpecialType != SpecialType.System_ValueType &&
            symbol.BaseType.SpecialType != SpecialType.System_Enum)
        {
            type.BaseType = symbol.BaseType.ToDisplayString();
        }

        // Interfaces
        type.Interfaces = [.. symbol.Interfaces
            .Select(i => i.ToDisplayString())
            .OrderBy(i => i)];

        // Members
        type.Members = BuildMembers(symbol);

        // XML docs
        type.Docs = ExtractDocumentation(symbol);

        // Enum members
        if (symbol.TypeKind == TypeKind.Enum)
        {
            type.EnumMembers = [.. symbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .Select(f => new CanonicalEnumMember
                {
                    Name = f.Name,
                    Value = Convert.ToInt64(f.ConstantValue),
                    Description = ExtractSummary(f),
                })];
        }

        // Attributes
        type.Attributes = ExtractShapeAttributes(symbol);

        return type;
    }

    private List<CanonicalMember> BuildMembers(INamedTypeSymbol type)
    {
        var members = new List<CanonicalMember>();

        foreach (var member in type.GetMembers().OrderBy(m => m.Name))
        {
            if (member.DeclaredAccessibility != Accessibility.Public ||
                member.IsImplicitlyDeclared ||
                ShouldExclude(member))
            {
                continue;
            }

            switch (member)
            {
                case IMethodSymbol method when method.MethodKind is
                    MethodKind.Ordinary or
                    MethodKind.Constructor or
                    MethodKind.UserDefinedOperator or
                    MethodKind.Conversion:
                    members.Add(BuildMethod(method));
                    break;

                case IPropertySymbol property:
                    members.Add(BuildProperty(property));
                    break;

                case IFieldSymbol field when type.TypeKind != TypeKind.Enum:
                    members.Add(BuildField(field));
                    break;

                case IEventSymbol evt:
                    members.Add(BuildEvent(evt));
                    break;
            }
        }

        return members;
    }

    private static CanonicalMember BuildMethod(IMethodSymbol method) => new()
    {
        Name = method.MethodKind == MethodKind.Constructor ? ".ctor" : method.Name,
        Kind = method.MethodKind == MethodKind.Constructor ? "constructor" : "method",
        Accessibility = method.DeclaredAccessibility.ToString().ToLowerInvariant(),
        IsStatic = method.IsStatic,
        IsAbstract = method.IsAbstract,
        IsVirtual = method.IsVirtual,
        IsOverride = method.IsOverride,
        IsAsync = method.IsAsync,
        ReturnType = method.ReturnsVoid ? "void" : method.ReturnType.ToDisplayString(),
        IsReturnNullable = method.ReturnType.NullableAnnotation == NullableAnnotation.Annotated,
        Parameters = [.. method.Parameters
            .Select(p => new CanonicalParameter
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(),
                IsNullable = p.Type.NullableAnnotation == NullableAnnotation.Annotated,
                IsOptional = p.IsOptional,
                DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue?.ToString() : null,
                Modifier = p.RefKind switch
                {
                    RefKind.Ref => "ref",
                    RefKind.Out => "out",
                    RefKind.In => "in",
                    RefKind.RefReadOnlyParameter => "ref readonly",
                    _ => p.IsParams ? "params" : null,
                },
            })],
        GenericParameters = method.IsGenericMethod
            ? [.. method.TypeParameters.Select(BuildGenericParameter)]
            : null,
        Signature = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        Docs = ExtractDocumentation(method),
        Attributes = ExtractShapeAttributes(method),
    };

    private static CanonicalMember BuildProperty(IPropertySymbol property) => new()
    {
        Name = property.IsIndexer ? "this[]" : property.Name,
        Kind = property.IsIndexer ? "indexer" : "property",
        Accessibility = property.DeclaredAccessibility.ToString().ToLowerInvariant(),
        IsStatic = property.IsStatic,
        IsAbstract = property.IsAbstract,
        IsVirtual = property.IsVirtual,
        IsOverride = property.IsOverride,
        ReturnType = property.Type.ToDisplayString(),
        IsReturnNullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated,
        Parameters = property.IsIndexer
            ? [.. property.Parameters.Select(p => new CanonicalParameter
            {
                Name = p.Name,
                Type = p.Type.ToDisplayString(),
                IsNullable = p.Type.NullableAnnotation == NullableAnnotation.Annotated,
            })]
            : null,
        Signature = property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        Docs = ExtractDocumentation(property),
        Attributes = ExtractShapeAttributes(property),
    };

    private static CanonicalMember BuildField(IFieldSymbol field) => new()
    {
        Name = field.Name,
        Kind = "field",
        Accessibility = field.DeclaredAccessibility.ToString().ToLowerInvariant(),
        IsStatic = field.IsStatic,
        ReturnType = field.Type.ToDisplayString(),
        IsReturnNullable = field.Type.NullableAnnotation == NullableAnnotation.Annotated,
        Signature = field.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        Docs = ExtractDocumentation(field),
        Attributes = ExtractShapeAttributes(field),
    };

    private static CanonicalMember BuildEvent(IEventSymbol evt) => new()
    {
        Name = evt.Name,
        Kind = "event",
        Accessibility = evt.DeclaredAccessibility.ToString().ToLowerInvariant(),
        IsStatic = evt.IsStatic,
        IsAbstract = evt.IsAbstract,
        IsVirtual = evt.IsVirtual,
        IsOverride = evt.IsOverride,
        ReturnType = evt.Type.ToDisplayString(),
        Signature = evt.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
        Docs = ExtractDocumentation(evt),
        Attributes = ExtractShapeAttributes(evt),
    };

    private static CanonicalGenericParameter BuildGenericParameter(ITypeParameterSymbol tp) => new()
    {
        Name = tp.Name,
        Constraints = GetConstraints(tp),
    };

    private static List<string> GetConstraints(ITypeParameterSymbol tp)
    {
        var constraints = new List<string>();

        if (tp.HasReferenceTypeConstraint) constraints.Add("class");
        if (tp.HasValueTypeConstraint) constraints.Add("struct");
        if (tp.HasUnmanagedTypeConstraint) constraints.Add("unmanaged");
        if (tp.HasNotNullConstraint) constraints.Add("notnull");
        if (tp.HasConstructorConstraint) constraints.Add("new()");

        foreach (var ct in tp.ConstraintTypes)
        {
            constraints.Add(ct.ToDisplayString());
        }

        return constraints;
    }

    private static CanonicalDocumentation? ExtractDocumentation(ISymbol symbol)
    {
        var xmlComment = symbol.GetDocumentationCommentXml();

        // Handle inheritdoc: resolve from base types/interfaces
        if (string.IsNullOrWhiteSpace(xmlComment) || xmlComment!.Contains("<inheritdoc"))
        {
            var inherited = ResolveInheritedDocumentation(symbol);
            if (inherited is not null)
            {
                // If we have partial docs with inheritdoc, merge; otherwise use inherited
                if (string.IsNullOrWhiteSpace(xmlComment))
                {
                    return inherited;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(xmlComment))
        {
            return null;
        }

        try
        {
            var doc = XDocument.Parse(xmlComment);
            var root = doc.Root;
            if (root is null) return null;

            var docs = new CanonicalDocumentation
            {
                Summary = ParseDocContent(root.Element("summary")),
                Remarks = ParseDocContent(root.Element("remarks")),
                Returns = ParseDocContent(root.Element("returns")),
                Value = ParseDocContent(root.Element("value")),
            };

            // Parameters
            var paramElements = root.Elements("param").ToList();
            if (paramElements.Count > 0)
            {
                docs.Parameters = new Dictionary<string, List<DocNode>>();
                foreach (var param in paramElements)
                {
                    var name = param.Attribute("name")?.Value;
                    if (name is not null)
                    {
                        docs.Parameters[name] = ParseDocContent(param) ?? [DocNode.TextNode("")];
                    }
                }
            }

            // Type parameters
            var typeParamElements = root.Elements("typeparam").ToList();
            if (typeParamElements.Count > 0)
            {
                docs.TypeParameters = new Dictionary<string, List<DocNode>>();
                foreach (var tp in typeParamElements)
                {
                    var name = tp.Attribute("name")?.Value;
                    if (name is not null)
                    {
                        docs.TypeParameters[name] = ParseDocContent(tp) ?? [DocNode.TextNode("")];
                    }
                }
            }

            // Exceptions
            var exceptionElements = root.Elements("exception").ToList();
            if (exceptionElements.Count > 0)
            {
                docs.Exceptions = [];
                foreach (var exc in exceptionElements)
                {
                    var cref = exc.Attribute("cref")?.Value;
                    if (cref is not null)
                    {
                        docs.Exceptions.Add(new DocException
                        {
                            Type = cref,
                            Description = ParseDocContent(exc) ?? [DocNode.TextNode("")],
                        });
                    }
                }
            }

            // Permissions
            var permissionElements = root.Elements("permission").ToList();
            if (permissionElements.Count > 0)
            {
                docs.Permissions = [];
                foreach (var perm in permissionElements)
                {
                    var cref = perm.Attribute("cref")?.Value;
                    if (cref is not null)
                    {
                        docs.Permissions.Add(new DocPermission
                        {
                            Type = cref,
                            Description = ParseDocContent(perm) ?? [DocNode.TextNode("")],
                        });
                    }
                }
            }

            // Examples
            var exampleElements = root.Elements("example").ToList();
            if (exampleElements.Count > 0)
            {
                docs.Examples = [];
                foreach (var example in exampleElements)
                {
                    var codeElement = example.Element("code");
                    if (codeElement is not null)
                    {
                        // Parse description from non-code child nodes
                        var descNodes = new List<DocNode>();
                        foreach (var node in example.Nodes())
                        {
                            if (node is XElement el && el.Name == "code") continue;
                            ParseXmlNode(node, descNodes);
                        }
                        NormalizeDocNodes(descNodes);

                        docs.Examples.Add(new CanonicalCodeExample
                        {
                            Language = codeElement.Attribute("language")?.Value ?? "csharp",
                            Region = codeElement.Attribute("region")?.Value,
                            Code = codeElement.Value.Trim(),
                            Description = descNodes.Count > 0 ? descNodes : null,
                        });
                    }
                }
            }

            // See also — supports both cref and href
            var seeAlsoElements = root.Elements("seealso").ToList();
            if (seeAlsoElements.Count > 0)
            {
                docs.SeeAlso = [.. seeAlsoElements
                    .Select(s => s.Attribute("cref")?.Value
                              ?? s.Attribute("href")?.Value
                              ?? s.Value)
                    .Where(s => !string.IsNullOrEmpty(s))];
            }

            return docs;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses an XML element's content into a list of rich documentation nodes,
    /// preserving see-cref, langword, code, paramref, typeparamref, para, and list elements.
    /// </summary>
    private static List<DocNode>? ParseDocContent(XElement? element)
    {
        if (element is null) return null;

        var nodes = new List<DocNode>();

        foreach (var child in element.Nodes())
        {
            ParseXmlNode(child, nodes);
        }

        NormalizeDocNodes(nodes);

        return nodes.Count > 0 ? nodes : null;
    }

    /// <summary>Recursively parses an XML node into DocNode list.</summary>
    private static void ParseXmlNode(XNode node, List<DocNode> result)
    {
        switch (node)
        {
            case XText textNode:
                var text = textNode.Value
                    .Replace("\r\n", "\n")
                    .Replace("\r", "\n");

                // Normalize internal whitespace: collapse runs of spaces/newlines
                var lines = text.Split('\n');
                var normalized = string.Join(" ",
                    lines.Select(l => l.Trim()).Where(l => l.Length > 0));

                if (normalized.Length > 0)
                {
                    result.Add(DocNode.TextNode(normalized));
                }
                break;

            case XElement el:
                switch (el.Name.LocalName)
                {
                    case "see":
                        var cref = el.Attribute("cref")?.Value;
                        var langword = el.Attribute("langword")?.Value;
                        var href = el.Attribute("href")?.Value;

                        if (cref is not null)
                        {
                            result.Add(DocNode.CrefNode(cref));
                        }
                        else if (langword is not null)
                        {
                            result.Add(DocNode.LangwordNode(langword));
                        }
                        else if (href is not null)
                        {
                            var linkText = el.Value.Trim();
                            result.Add(DocNode.HrefNode(href,
                                string.IsNullOrEmpty(linkText) ? null : linkText));
                        }
                        break;

                    case "c":
                        result.Add(DocNode.CodeNode(el.Value));
                        break;

                    case "code":
                        // Standalone <code> block (not inside <example>)
                        result.Add(DocNode.CodeblockNode(
                            el.Value.Trim(),
                            el.Attribute("language")?.Value));
                        break;

                    case "paramref":
                        var paramName = el.Attribute("name")?.Value;
                        if (paramName is not null)
                        {
                            result.Add(DocNode.ParamrefNode(paramName));
                        }
                        break;

                    case "typeparamref":
                        var typeParamName = el.Attribute("name")?.Value;
                        if (typeParamName is not null)
                        {
                            result.Add(DocNode.TypeparamrefNode(typeParamName));
                        }
                        break;

                    case "para":
                        var paraChildren = ParseDocContent(el);
                        if (paraChildren is { Count: > 0 })
                        {
                            result.Add(DocNode.ParaNode(paraChildren));
                        }
                        break;

                    case "list":
                        var listStyle = el.Attribute("type")?.Value ?? "bullet";
                        var items = new List<DocListItem>();

                        // Parse optional listheader
                        DocListItem? listHeader = null;
                        var listheaderEl = el.Element("listheader");
                        if (listheaderEl is not null)
                        {
                            var headerTermEl = listheaderEl.Element("term");
                            var headerDescEl = listheaderEl.Element("description");
                            listHeader = new DocListItem
                            {
                                Term = ParseDocContent(headerTermEl),
                                Description = ParseDocContent(headerDescEl) ?? [DocNode.TextNode(listheaderEl.Value.Trim())],
                            };
                        }

                        foreach (var item in el.Elements("item"))
                        {
                            var termEl = item.Element("term");
                            var descEl = item.Element("description");
                            items.Add(new DocListItem
                            {
                                Term = ParseDocContent(termEl),
                                Description = ParseDocContent(descEl) ?? [DocNode.TextNode(item.Value.Trim())],
                            });
                        }
                        if (items.Count > 0)
                        {
                            result.Add(DocNode.ListNode(listStyle, items, listHeader));
                        }
                        break;

                    case "note":
                        var noteType = el.Attribute("type")?.Value ?? "note";
                        var noteChildren = ParseDocContent(el);
                        if (noteChildren is { Count: > 0 })
                        {
                            result.Add(DocNode.NoteNode(noteType, noteChildren));
                        }
                        break;

                    default:
                        // Unknown element — recurse into its children to preserve nested content
                        foreach (var child in el.Nodes())
                        {
                            ParseXmlNode(child, result);
                        }
                        break;
                }
                break;
        }
    }

    /// <summary>
    /// Removes leading/trailing whitespace text nodes and collapses adjacent text nodes.
    /// </summary>
    private static void NormalizeDocNodes(List<DocNode> nodes)
    {
        // Remove leading empty text
        while (nodes.Count > 0 &&
               nodes[0].Kind == "text" &&
               string.IsNullOrWhiteSpace(nodes[0].Text))
        {
            nodes.RemoveAt(0);
        }

        // Remove trailing empty text
        while (nodes.Count > 0 &&
               nodes[nodes.Count - 1].Kind == "text" &&
               string.IsNullOrWhiteSpace(nodes[nodes.Count - 1].Text))
        {
            nodes.RemoveAt(nodes.Count - 1);
        }

        // Merge adjacent text nodes
        for (int i = nodes.Count - 1; i > 0; i--)
        {
            if (nodes[i].Kind == "text" && nodes[i - 1].Kind == "text")
            {
                nodes[i - 1] = DocNode.TextNode(nodes[i - 1].Text + " " + nodes[i].Text);
                nodes.RemoveAt(i);
            }
        }
    }

    /// <summary>Flattens doc nodes to plain text for contexts that need a string.</summary>
    internal static string FlattenDocNodesToText(List<DocNode>? nodes)
    {
        if (nodes is null || nodes.Count == 0) return "";

        var sb = new System.Text.StringBuilder();
        FlattenNodes(nodes, sb);
        return sb.ToString().Trim();
    }

    private static void FlattenNodes(List<DocNode> nodes, System.Text.StringBuilder sb)
    {
        foreach (var node in nodes)
        {
            switch (node.Kind)
            {
                case "text":
                    sb.Append(node.Text);
                    break;
                case "code":
                    sb.Append(node.Text);
                    break;
                case "codeblock":
                    sb.Append(node.Text);
                    break;
                case "cref":
                    // Extract short name from cref like "T:SampleApi.Customer" -> "Customer"
                    var crefVal = node.Value ?? "";
                    var lastDot = crefVal.LastIndexOf('.');
                    sb.Append(lastDot >= 0 ? crefVal.Substring(lastDot + 1) : crefVal);
                    break;
                case "href":
                    sb.Append(node.Text ?? node.Value);
                    break;
                case "langword":
                    sb.Append(node.Value);
                    break;
                case "paramref":
                case "typeparamref":
                    sb.Append(node.Value);
                    break;
                case "para":
                case "note":
                    sb.Append(" ");
                    if (node.Children is not null)
                        FlattenNodes(node.Children, sb);
                    sb.Append(" ");
                    break;
                case "list":
                    if (node.Items is not null)
                    {
                        foreach (var item in node.Items)
                        {
                            sb.Append(" ");
                            FlattenNodes(item.Description, sb);
                        }
                    }
                    break;
            }
        }
    }

    private static string? ExtractSummary(ISymbol symbol)
    {
        var xmlComment = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xmlComment)) return null;

        try
        {
            var doc = XDocument.Parse(xmlComment);
            var nodes = ParseDocContent(doc.Root?.Element("summary"));
            return FlattenDocNodesToText(nodes);
        }
        catch
        {
            return null;
        }
    }

    private static List<CanonicalAttribute>? ExtractShapeAttributes(ISymbol symbol)
    {
        var relevant = symbol.GetAttributes()
            .Where(a => IsShapeAffectingAttribute(a.AttributeClass?.Name ?? ""))
            .Select(a => new CanonicalAttribute
            {
                Name = a.AttributeClass?.Name ?? "",
                Arguments = a.NamedArguments.Length > 0
                    ? a.NamedArguments.ToDictionary(n => n.Key, n => n.Value.Value?.ToString() ?? "")
                    : null,
            })
            .ToList();

        return relevant.Count > 0 ? relevant : null;
    }

    private static bool IsShapeAffectingAttribute(string name) => name is
        "ObsoleteAttribute" or
        "FlagsAttribute" or
        "JsonConverterAttribute" or
        "JsonDerivedTypeAttribute" or
        "JsonPolymorphicAttribute" or
        "JsonNumberHandlingAttribute" or
        "JsonRequiredAttribute" or
        "ApiContractAttribute";

    private static bool ShouldExclude(ISymbol symbol)
    {
        return symbol.GetAttributes()
            .Any(a =>
            {
                if (a.AttributeClass?.Name != "ApiContractAttribute" &&
                    a.AttributeClass?.ToDisplayString() != "ApiContracts.ApiContractAttribute")
                {
                    return false;
                }

                return a.NamedArguments.Any(n => n.Key == "Ignore" && n.Value.Value is true);
            });
    }

    /// <summary>
    /// Resolves inherited documentation from base types and interfaces for inheritdoc support.
    /// </summary>
    private static CanonicalDocumentation? ResolveInheritedDocumentation(ISymbol symbol)
    {
        // For methods/properties, look at the overridden member first, then interface implementations
        if (symbol is IMethodSymbol method)
        {
            // Check overridden method
            if (method.OverriddenMethod is not null)
            {
                var docs = ExtractDocumentation(method.OverriddenMethod);
                if (docs is not null) return docs;
            }

            // Check interface implementations
            foreach (var iface in method.ContainingType?.Interfaces ?? ImmutableArray<INamedTypeSymbol>.Empty)
            {
                var ifaceMember = iface.GetMembers(method.Name)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => ParametersMatch(m, method));
                if (ifaceMember is not null)
                {
                    var docs = ExtractDocumentation(ifaceMember);
                    if (docs is not null) return docs;
                }
            }
        }
        else if (symbol is IPropertySymbol property)
        {
            if (property.OverriddenProperty is not null)
            {
                var docs = ExtractDocumentation(property.OverriddenProperty);
                if (docs is not null) return docs;
            }

            foreach (var iface in property.ContainingType?.Interfaces ?? ImmutableArray<INamedTypeSymbol>.Empty)
            {
                var ifaceMember = iface.GetMembers(property.Name)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault();
                if (ifaceMember is not null)
                {
                    var docs = ExtractDocumentation(ifaceMember);
                    if (docs is not null) return docs;
                }
            }
        }
        else if (symbol is INamedTypeSymbol type)
        {
            // For types, check base type
            if (type.BaseType is not null &&
                type.BaseType.SpecialType == SpecialType.None)
            {
                var docs = ExtractDocumentation(type.BaseType);
                if (docs is not null) return docs;
            }
        }

        return null;
    }

    private static bool ParametersMatch(IMethodSymbol a, IMethodSymbol b)
    {
        if (a.Parameters.Length != b.Parameters.Length) return false;

        for (int i = 0; i < a.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(
                a.Parameters[i].Type, b.Parameters[i].Type))
            {
                return false;
            }
        }

        return true;
    }
}
