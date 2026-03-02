// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Emits the assembly schema JSON with deterministic formatting.
/// The output contains only public types/members with parsed docs and is
/// ordered so that two identical API surfaces always produce identical JSON.
/// </summary>
internal static class SchemaEmitter
{
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = true,
    };

    public static string EmitAssemblySchema(
        string assemblyName,
        string assemblyVersion,
        string targetFramework,
        List<CanonicalType> types,
        string apiHash,
        AssemblyConfig config,
        string? signatureValue = null)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, s_writerOptions))
        {
            writer.WriteStartObject();
            writer.WriteString("$schema", "https://ievangelist.github.io/api.contracts/schemas/api-schema.json");
            writer.WriteString("schemaVersion", "1.0.0");

            writer.WriteStartObject("package");
            writer.WriteString("name", assemblyName);
            writer.WriteString("version", assemblyVersion);
            writer.WriteString("targetFramework", targetFramework);
            writer.WriteEndObject();

            writer.WriteString("apiHash", apiHash);

            // Signature envelope (optional)
            if (signatureValue is not null && config.SigningKeyId is not null)
            {
                writer.WriteStartObject("signature");
                writer.WriteString("algorithm", "RSA-SHA256");
                writer.WriteString("publicKeyId", config.SigningKeyId);
                writer.WriteString("value", signatureValue);
                writer.WriteEndObject();
            }

            // Types — deterministically sorted
            writer.WriteStartArray("types");

            var sortedTypes = types
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();

            foreach (var type in sortedTypes)
            {
                EmitType(writer, type);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void EmitType(Utf8JsonWriter writer, CanonicalType type)
    {
        writer.WriteStartObject();
        writer.WriteString("name", type.Name);
        writer.WriteString("fullName", type.FullName);
        writer.WriteString("namespace", type.Namespace);
        writer.WriteString("kind", type.Kind);
        writer.WriteString("accessibility", type.Accessibility);

        // Boolean flags — only emit when true
        if (type.IsAbstract) writer.WriteBoolean("isAbstract", true);
        if (type.IsSealed) writer.WriteBoolean("isSealed", true);
        if (type.IsStatic) writer.WriteBoolean("isStatic", true);
        if (type.IsGeneric) writer.WriteBoolean("isGeneric", true);

        // Generic parameters
        if (type.GenericParameters is { Count: > 0 })
        {
            writer.WriteStartArray("genericParameters");
            foreach (var gp in type.GenericParameters)
            {
                writer.WriteStartObject();
                writer.WriteString("name", gp.Name);
                if (gp.Constraints.Count > 0)
                {
                    writer.WriteStartArray("constraints");
                    foreach (var c in gp.Constraints)
                    {
                        writer.WriteStringValue(c);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        // Base type
        if (type.BaseType is not null)
        {
            writer.WriteString("baseType", type.BaseType);
        }

        // Interfaces
        if (type.Interfaces.Count > 0)
        {
            writer.WriteStartArray("interfaces");
            foreach (var iface in type.Interfaces)
            {
                writer.WriteStringValue(iface);
            }
            writer.WriteEndArray();
        }

        // Docs
        if (type.Docs is not null)
        {
            writer.WritePropertyName("docs");
            EmitDocumentation(writer, type.Docs);
        }

        // Enum members
        if (type.EnumMembers is { Count: > 0 })
        {
            writer.WriteStartArray("enumMembers");
            foreach (var em in type.EnumMembers)
            {
                writer.WriteStartObject();
                writer.WriteString("name", em.Name);
                writer.WriteNumber("value", em.Value);
                if (em.Description is not null)
                {
                    writer.WriteString("description", em.Description);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        // Attributes
        if (type.Attributes is { Count: > 0 })
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in type.Attributes)
            {
                writer.WriteStartObject();
                writer.WriteString("name", attr.Name);
                if (attr.Arguments is { Count: > 0 })
                {
                    writer.WriteStartObject("arguments");
                    foreach (var kvp in attr.Arguments.OrderBy(a => a.Key))
                    {
                        writer.WriteString(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        // Members — deterministically sorted
        writer.WriteStartArray("members");
        var sortedMembers = type.Members
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.Name)
            .ToList();

        foreach (var member in sortedMembers)
        {
            EmitMember(writer, member);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static void EmitMember(Utf8JsonWriter writer, CanonicalMember member)
    {
        writer.WriteStartObject();
        writer.WriteString("name", member.Name);
        writer.WriteString("kind", member.Kind);
        writer.WriteString("accessibility", member.Accessibility);
        writer.WriteString("signature", member.Signature);

        if (member.ReturnType is not null)
        {
            writer.WriteString("returnType", member.ReturnType);
            if (member.IsReturnNullable)
            {
                writer.WriteBoolean("isReturnNullable", true);
            }
        }

        if (member.IsStatic) writer.WriteBoolean("isStatic", true);
        if (member.IsAbstract) writer.WriteBoolean("isAbstract", true);
        if (member.IsVirtual) writer.WriteBoolean("isVirtual", true);
        if (member.IsOverride) writer.WriteBoolean("isOverride", true);
        if (member.IsAsync) writer.WriteBoolean("isAsync", true);

        // Parameters
        if (member.Parameters is { Count: > 0 })
        {
            writer.WriteStartArray("parameters");
            foreach (var p in member.Parameters)
            {
                writer.WriteStartObject();
                writer.WriteString("name", p.Name);
                writer.WriteString("type", p.Type);
                if (p.IsNullable) writer.WriteBoolean("isNullable", true);
                if (p.IsOptional)
                {
                    writer.WriteBoolean("isOptional", true);
                    if (p.DefaultValue is not null) writer.WriteString("defaultValue", p.DefaultValue);
                }
                if (p.Modifier is not null) writer.WriteString("modifier", p.Modifier);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        // Attributes
        if (member.Attributes is { Count: > 0 })
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in member.Attributes)
            {
                writer.WriteStartObject();
                writer.WriteString("name", attr.Name);
                if (attr.Arguments is { Count: > 0 })
                {
                    writer.WriteStartObject("arguments");
                    foreach (var kvp in attr.Arguments.OrderBy(a => a.Key))
                    {
                        writer.WriteString(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        // Docs
        if (member.Docs is not null)
        {
            writer.WritePropertyName("docs");
            EmitDocumentation(writer, member.Docs);
        }

        writer.WriteEndObject();
    }

    private static void EmitDocumentation(Utf8JsonWriter writer, CanonicalDocumentation docs)
    {
        writer.WriteStartObject();

        if (docs.Summary is not null)
        {
            writer.WritePropertyName("summary");
            EmitDocNodes(writer, docs.Summary);
        }
        if (docs.Remarks is not null)
        {
            writer.WritePropertyName("remarks");
            EmitDocNodes(writer, docs.Remarks);
        }
        if (docs.Returns is not null)
        {
            writer.WritePropertyName("returns");
            EmitDocNodes(writer, docs.Returns);
        }
        if (docs.Value is not null)
        {
            writer.WritePropertyName("value");
            EmitDocNodes(writer, docs.Value);
        }

        if (docs.Parameters is { Count: > 0 })
        {
            writer.WriteStartObject("parameters");
            foreach (var kvp in docs.Parameters.OrderBy(p => p.Key))
            {
                writer.WritePropertyName(kvp.Key);
                EmitDocNodes(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        if (docs.TypeParameters is { Count: > 0 })
        {
            writer.WriteStartObject("typeParameters");
            foreach (var kvp in docs.TypeParameters.OrderBy(p => p.Key))
            {
                writer.WritePropertyName(kvp.Key);
                EmitDocNodes(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        if (docs.Exceptions is { Count: > 0 })
        {
            writer.WriteStartArray("exceptions");
            foreach (var exc in docs.Exceptions)
            {
                writer.WriteStartObject();
                writer.WriteString("type", exc.Type);
                writer.WritePropertyName("description");
                EmitDocNodes(writer, exc.Description);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (docs.Permissions is { Count: > 0 })
        {
            writer.WriteStartArray("permissions");
            foreach (var perm in docs.Permissions)
            {
                writer.WriteStartObject();
                writer.WriteString("type", perm.Type);
                writer.WritePropertyName("description");
                EmitDocNodes(writer, perm.Description);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (docs.Examples is { Count: > 0 })
        {
            writer.WriteStartArray("examples");
            foreach (var ex in docs.Examples)
            {
                writer.WriteStartObject();
                writer.WriteString("language", ex.Language);
                if (ex.Region is not null) writer.WriteString("region", ex.Region);
                writer.WriteString("code", ex.Code);
                if (ex.Description is { Count: > 0 })
                {
                    writer.WritePropertyName("description");
                    EmitDocNodes(writer, ex.Description);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (docs.SeeAlso is { Count: > 0 })
        {
            writer.WriteStartArray("seeAlso");
            foreach (var s in docs.SeeAlso)
            {
                writer.WriteStringValue(s);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static void EmitDocNodes(Utf8JsonWriter writer, List<DocNode> nodes)
    {
        writer.WriteStartArray();
        foreach (var node in nodes)
        {
            EmitDocNode(writer, node);
        }
        writer.WriteEndArray();
    }

    private static void EmitDocNode(Utf8JsonWriter writer, DocNode node)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", node.Kind);

        switch (node.Kind)
        {
            case "text":
            case "code":
                if (node.Text is not null)
                    writer.WriteString("text", node.Text);
                break;

            case "codeblock":
                if (node.Text is not null)
                    writer.WriteString("text", node.Text);
                if (node.Language is not null)
                    writer.WriteString("language", node.Language);
                break;

            case "cref":
            case "langword":
            case "paramref":
            case "typeparamref":
                if (node.Value is not null)
                    writer.WriteString("value", node.Value);
                break;

            case "href":
                if (node.Value is not null)
                    writer.WriteString("value", node.Value);
                if (node.Text is not null)
                    writer.WriteString("text", node.Text);
                break;

            case "para":
                if (node.Children is { Count: > 0 })
                {
                    writer.WritePropertyName("children");
                    EmitDocNodes(writer, node.Children);
                }
                break;

            case "note":
                if (node.Value is not null)
                    writer.WriteString("value", node.Value);
                if (node.Children is { Count: > 0 })
                {
                    writer.WritePropertyName("children");
                    EmitDocNodes(writer, node.Children);
                }
                break;

            case "list":
                if (node.Style is not null)
                    writer.WriteString("style", node.Style);
                if (node.Header is not null)
                {
                    writer.WriteStartObject("header");
                    if (node.Header.Term is { Count: > 0 })
                    {
                        writer.WritePropertyName("term");
                        EmitDocNodes(writer, node.Header.Term);
                    }
                    writer.WritePropertyName("description");
                    EmitDocNodes(writer, node.Header.Description);
                    writer.WriteEndObject();
                }
                if (node.Items is { Count: > 0 })
                {
                    writer.WriteStartArray("items");
                    foreach (var item in node.Items)
                    {
                        writer.WriteStartObject();
                        if (item.Term is { Count: > 0 })
                        {
                            writer.WritePropertyName("term");
                            EmitDocNodes(writer, item.Term);
                        }
                        writer.WritePropertyName("description");
                        EmitDocNodes(writer, item.Description);
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
                break;
        }

        writer.WriteEndObject();
    }
}
