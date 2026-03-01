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

        // JSON contract
        type.Json = BuildJsonContract(symbol);

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
                Summary = NormalizeXmlText(root.Element("summary")),
                Remarks = NormalizeXmlText(root.Element("remarks")),
                Returns = NormalizeXmlText(root.Element("returns")),
            };

            // Parameters
            var paramElements = root.Elements("param").ToList();
            if (paramElements.Count > 0)
            {
                docs.Parameters = new Dictionary<string, string>();
                foreach (var param in paramElements)
                {
                    var name = param.Attribute("name")?.Value;
                    if (name is not null)
                    {
                        docs.Parameters[name] = NormalizeXmlText(param) ?? "";
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
                        docs.Examples.Add(new CanonicalCodeExample
                        {
                            Language = codeElement.Attribute("language")?.Value ?? "csharp",
                            Region = codeElement.Attribute("region")?.Value,
                            Code = codeElement.Value.Trim(),
                            Description = NormalizeXmlText(example.Elements()
                                .Where(e => e.Name != "code")
                                .FirstOrDefault()),
                        });
                    }
                }
            }

            // See also
            var seeAlsoElements = root.Elements("seealso").ToList();
            if (seeAlsoElements.Count > 0)
            {
                docs.SeeAlso = [.. seeAlsoElements
                    .Select(s => s.Attribute("cref")?.Value ?? s.Value)
                    .Where(s => !string.IsNullOrEmpty(s))];
            }

            return docs;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSummary(ISymbol symbol)
    {
        var xmlComment = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xmlComment)) return null;

        try
        {
            var doc = XDocument.Parse(xmlComment);
            return NormalizeXmlText(doc.Root?.Element("summary"));
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeXmlText(XElement? element)
    {
        if (element is null) return null;

        var text = element.Value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        // Normalize whitespace
        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);

        var result = string.Join(" ", lines);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static CanonicalJsonContract? BuildJsonContract(INamedTypeSymbol type)
    {
        if (type.TypeKind is TypeKind.Interface or TypeKind.Enum or TypeKind.Delegate)
        {
            return null;
        }

        var properties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
            .Select(BuildJsonProperty)
            .Where(p => p is not null)
            .Cast<CanonicalJsonProperty>()
            .ToList();

        if (properties.Count == 0)
        {
            return null;
        }

        var useCamelCase = type.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "JsonSourceGenerationOptionsAttribute" ||
                       a.AttributeClass?.ToDisplayString().Contains("JsonSerializerOptions") == true);

        return new CanonicalJsonContract
        {
            ContractType = "object",
            Properties = properties,
            UseCamelCase = true, // Default System.Text.Json web defaults
        };
    }

    private static CanonicalJsonProperty? BuildJsonProperty(IPropertySymbol property)
    {
        // Check for JsonIgnore
        var isIgnored = property.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "JsonIgnoreAttribute");

        // Get JSON property name
        var jsonNameAttr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "JsonPropertyNameAttribute");

        var jsonName = jsonNameAttr?.ConstructorArguments.FirstOrDefault().Value as string
            ?? ToCamelCase(property.Name);

        // Check for required
        var isRequired = property.IsRequired ||
            property.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "JsonRequiredAttribute" ||
                a.AttributeClass?.Name == "RequiredAttribute");

        return new CanonicalJsonProperty
        {
            ClrName = property.Name,
            JsonName = jsonName,
            JsonType = MapToJsonType(property.Type),
            Ignored = isIgnored,
            Nullable = property.Type.NullableAnnotation == NullableAnnotation.Annotated ||
                       property.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
            Required = isRequired,
            ClrType = property.Type.ToDisplayString(),
            Description = ExtractSummary(property),
        };
    }

    private static string MapToJsonType(ITypeSymbol type)
    {
        var displayString = type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();

        return displayString switch
        {
            "string" => "string",
            "bool" => "boolean",
            "int" or "long" or "short" or "byte" or
            "uint" or "ulong" or "ushort" or "sbyte" or
            "float" or "double" or "decimal" => "number",
            "System.DateTime" or "System.DateTimeOffset" or "System.DateOnly" => "string",
            "System.TimeSpan" or "System.TimeOnly" => "string",
            "System.Guid" => "string",
            "System.Uri" => "string",
            _ when type.TypeKind == TypeKind.Enum => "string",
            _ when type.TypeKind == TypeKind.Array => "array",
            _ when IsCollectionType(type) => "array",
            _ when type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct => "object",
            _ => "object",
        };
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        return type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>" &&
            i.ToDisplayString() != "System.Collections.Generic.IEnumerable<char>");
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (char.IsLower(name[0])) return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
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
