// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Produces deterministic canonical JSON for hashing.
/// Properties are sorted, no whitespace, UTF-8.
/// </summary>
internal static class CanonicalSerializer
{
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    public static string SerializeForHashing(List<CanonicalType> types)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, s_writerOptions))
        {
            writer.WriteStartArray();

            var sortedTypes = types
                .OrderBy(t => t.Namespace)
                .ThenBy(t => t.Name)
                .ToList();

            foreach (var type in sortedTypes)
            {
                SerializeType(writer, type);
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void SerializeType(Utf8JsonWriter writer, CanonicalType type)
    {
        writer.WriteStartObject();
        writer.WriteString("accessibility", type.Accessibility);

        if (type.Attributes is { Count: > 0 })
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in type.Attributes)
            {
                SerializeAttribute(writer, attr);
            }
            writer.WriteEndArray();
        }

        if (type.BaseType is not null)
        {
            writer.WriteString("baseType", type.BaseType);
        }

        if (type.EnumMembers is { Count: > 0 })
        {
            writer.WriteStartArray("enumMembers");
            foreach (var em in type.EnumMembers)
            {
                writer.WriteStartObject();
                writer.WriteString("name", em.Name);
                writer.WriteNumber("value", em.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteString("fullName", type.FullName);

        if (type.GenericParameters is { Count: > 0 })
        {
            writer.WriteStartArray("genericParameters");
            foreach (var gp in type.GenericParameters)
            {
                writer.WriteStartObject();
                if (gp.Constraints.Count > 0)
                {
                    writer.WriteStartArray("constraints");
                    foreach (var c in gp.Constraints)
                    {
                        writer.WriteStringValue(c);
                    }
                    writer.WriteEndArray();
                }
                writer.WriteString("name", gp.Name);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        if (type.Interfaces.Count > 0)
        {
            writer.WriteStartArray("interfaces");
            foreach (var iface in type.Interfaces)
            {
                writer.WriteStringValue(iface);
            }
            writer.WriteEndArray();
        }

        writer.WriteBoolean("isAbstract", type.IsAbstract);
        writer.WriteBoolean("isGeneric", type.IsGeneric);
        writer.WriteBoolean("isSealed", type.IsSealed);
        writer.WriteBoolean("isStatic", type.IsStatic);

        writer.WriteString("kind", type.Kind);

        writer.WriteStartArray("members");
        var sortedMembers = type.Members
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.Name)
            .ThenBy(m => m.Signature)
            .ToList();

        foreach (var member in sortedMembers)
        {
            SerializeMember(writer, member);
        }
        writer.WriteEndArray();

        writer.WriteString("name", type.Name);
        writer.WriteString("namespace", type.Namespace);

        // Docs contribute to hash (excluding code sample content)
        if (type.Docs is not null)
        {
            writer.WritePropertyName("docs");
            SerializeDocsForHash(writer, type.Docs);
        }

        writer.WriteEndObject();
    }

    private static void SerializeMember(Utf8JsonWriter writer, CanonicalMember member)
    {
        writer.WriteStartObject();
        writer.WriteString("accessibility", member.Accessibility);

        if (member.Attributes is { Count: > 0 })
        {
            writer.WriteStartArray("attributes");
            foreach (var attr in member.Attributes)
            {
                SerializeAttribute(writer, attr);
            }
            writer.WriteEndArray();
        }

        if (member.Docs is not null)
        {
            writer.WritePropertyName("docs");
            SerializeDocsForHash(writer, member.Docs);
        }

        if (member.GenericParameters is { Count: > 0 })
        {
            writer.WriteStartArray("genericParameters");
            foreach (var gp in member.GenericParameters)
            {
                writer.WriteStartObject();
                writer.WriteString("name", gp.Name);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        writer.WriteBoolean("isAbstract", member.IsAbstract);
        writer.WriteBoolean("isAsync", member.IsAsync);
        writer.WriteBoolean("isOverride", member.IsOverride);
        writer.WriteBoolean("isReturnNullable", member.IsReturnNullable);
        writer.WriteBoolean("isStatic", member.IsStatic);
        writer.WriteBoolean("isVirtual", member.IsVirtual);

        writer.WriteString("kind", member.Kind);
        writer.WriteString("name", member.Name);

        if (member.Parameters is { Count: > 0 })
        {
            writer.WriteStartArray("parameters");
            foreach (var param in member.Parameters)
            {
                SerializeParameter(writer, param);
            }
            writer.WriteEndArray();
        }

        if (member.ReturnType is not null)
        {
            writer.WriteString("returnType", member.ReturnType);
        }

        writer.WriteString("signature", member.Signature);
        writer.WriteEndObject();
    }

    private static void SerializeParameter(Utf8JsonWriter writer, CanonicalParameter param)
    {
        writer.WriteStartObject();
        if (param.DefaultValue is not null)
        {
            writer.WriteString("defaultValue", param.DefaultValue);
        }
        writer.WriteBoolean("isNullable", param.IsNullable);
        writer.WriteBoolean("isOptional", param.IsOptional);
        if (param.Modifier is not null)
        {
            writer.WriteString("modifier", param.Modifier);
        }
        writer.WriteString("name", param.Name);
        writer.WriteString("type", param.Type);
        writer.WriteEndObject();
    }

    private static void SerializeDocsForHash(Utf8JsonWriter writer, CanonicalDocumentation docs)
    {
        // Docs contribute to hash excluding code sample content
        writer.WriteStartObject();

        if (docs.Parameters is { Count: > 0 })
        {
            writer.WriteStartObject("parameters");
            foreach (var kvp in docs.Parameters.OrderBy(k => k.Key))
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }
            writer.WriteEndObject();
        }
        if (docs.Remarks is not null)
        {
            writer.WriteString("remarks", docs.Remarks);
        }
        if (docs.Returns is not null)
        {
            writer.WriteString("returns", docs.Returns);
        }
        if (docs.Summary is not null)
        {
            writer.WriteString("summary", docs.Summary);
        }
        // Note: Examples content excluded from hash per spec
        // Note: SeeAlso excluded from hash per spec

        writer.WriteEndObject();
    }

    private static void SerializeAttribute(Utf8JsonWriter writer, CanonicalAttribute attr)
    {
        writer.WriteStartObject();
        if (attr.Arguments is { Count: > 0 })
        {
            writer.WriteStartObject("arguments");
            foreach (var kvp in attr.Arguments.OrderBy(k => k.Key))
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }
            writer.WriteEndObject();
        }
        writer.WriteString("name", attr.Name);
        writer.WriteEndObject();
    }
}
