// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Produces deterministic canonical JSON for hashing.
/// Properties are sorted, no whitespace, UTF-8.
/// </summary>
internal static class CanonicalSerializer
{
    public static string SerializeForHashing(List<CanonicalType> types)
    {
        var sb = new StringBuilder();
        sb.Append('[');

        var sortedTypes = types
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Name)
            .ToList();

        for (int i = 0; i < sortedTypes.Count; i++)
        {
            if (i > 0) sb.Append(',');
            SerializeType(sb, sortedTypes[i]);
        }

        sb.Append(']');
        return sb.ToString();
    }

    private static void SerializeType(StringBuilder sb, CanonicalType type)
    {
        sb.Append('{');
        AppendProperty(sb, "accessibility", type.Accessibility);
        sb.Append(',');

        if (type.Attributes is { Count: > 0 })
        {
            AppendKey(sb, "attributes");
            sb.Append('[');
            for (int i = 0; i < type.Attributes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                SerializeAttribute(sb, type.Attributes[i]);
            }
            sb.Append("],");
        }

        if (type.BaseType is not null)
        {
            AppendProperty(sb, "baseType", type.BaseType);
            sb.Append(',');
        }

        if (type.EnumMembers is { Count: > 0 })
        {
            AppendKey(sb, "enumMembers");
            sb.Append('[');
            for (int i = 0; i < type.EnumMembers.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('{');
                AppendProperty(sb, "name", type.EnumMembers[i].Name);
                sb.Append(',');
                AppendKey(sb, "value");
                sb.Append(type.EnumMembers[i].Value);
                sb.Append('}');
            }
            sb.Append("],");
        }

        AppendProperty(sb, "fullName", type.FullName);
        sb.Append(',');

        if (type.GenericParameters is { Count: > 0 })
        {
            AppendKey(sb, "genericParameters");
            sb.Append('[');
            for (int i = 0; i < type.GenericParameters.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('{');
                if (type.GenericParameters[i].Constraints.Count > 0)
                {
                    AppendKey(sb, "constraints");
                    SerializeStringArray(sb, type.GenericParameters[i].Constraints);
                    sb.Append(',');
                }
                AppendProperty(sb, "name", type.GenericParameters[i].Name);
                sb.Append('}');
            }
            sb.Append("],");
        }

        if (type.Interfaces.Count > 0)
        {
            AppendKey(sb, "interfaces");
            SerializeStringArray(sb, type.Interfaces);
            sb.Append(',');
        }

        AppendProperty(sb, "isAbstract", type.IsAbstract);
        sb.Append(',');
        AppendProperty(sb, "isGeneric", type.IsGeneric);
        sb.Append(',');
        AppendProperty(sb, "isSealed", type.IsSealed);
        sb.Append(',');
        AppendProperty(sb, "isStatic", type.IsStatic);
        sb.Append(',');

        if (type.Json is not null)
        {
            AppendKey(sb, "json");
            SerializeJsonContract(sb, type.Json);
            sb.Append(',');
        }

        AppendProperty(sb, "kind", type.Kind);
        sb.Append(',');

        AppendKey(sb, "members");
        sb.Append('[');
        var sortedMembers = type.Members
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.Name)
            .ThenBy(m => m.Signature)
            .ToList();

        for (int i = 0; i < sortedMembers.Count; i++)
        {
            if (i > 0) sb.Append(',');
            SerializeMember(sb, sortedMembers[i]);
        }
        sb.Append("],");

        AppendProperty(sb, "name", type.Name);
        sb.Append(',');
        AppendProperty(sb, "namespace", type.Namespace);

        // AI metadata contributes to hash
        if (type.AI is not null)
        {
            sb.Append(',');
            AppendKey(sb, "ai");
            SerializeAIMetadata(sb, type.AI);
        }

        // Docs contribute to hash (excluding code sample content)
        if (type.Docs is not null)
        {
            sb.Append(',');
            AppendKey(sb, "docs");
            SerializeDocsForHash(sb, type.Docs);
        }

        sb.Append('}');
    }

    private static void SerializeMember(StringBuilder sb, CanonicalMember member)
    {
        sb.Append('{');
        AppendProperty(sb, "accessibility", member.Accessibility);
        sb.Append(',');

        if (member.AI is not null)
        {
            AppendKey(sb, "ai");
            SerializeAIMetadata(sb, member.AI);
            sb.Append(',');
        }

        if (member.Attributes is { Count: > 0 })
        {
            AppendKey(sb, "attributes");
            sb.Append('[');
            for (int i = 0; i < member.Attributes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                SerializeAttribute(sb, member.Attributes[i]);
            }
            sb.Append("],");
        }

        if (member.Docs is not null)
        {
            AppendKey(sb, "docs");
            SerializeDocsForHash(sb, member.Docs);
            sb.Append(',');
        }

        if (member.GenericParameters is { Count: > 0 })
        {
            AppendKey(sb, "genericParameters");
            sb.Append('[');
            for (int i = 0; i < member.GenericParameters.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('{');
                AppendProperty(sb, "name", member.GenericParameters[i].Name);
                sb.Append('}');
            }
            sb.Append("],");
        }

        AppendProperty(sb, "isAbstract", member.IsAbstract);
        sb.Append(',');
        AppendProperty(sb, "isAsync", member.IsAsync);
        sb.Append(',');
        AppendProperty(sb, "isOverride", member.IsOverride);
        sb.Append(',');
        AppendProperty(sb, "isReturnNullable", member.IsReturnNullable);
        sb.Append(',');
        AppendProperty(sb, "isStatic", member.IsStatic);
        sb.Append(',');
        AppendProperty(sb, "isVirtual", member.IsVirtual);
        sb.Append(',');

        if (member.Json is not null)
        {
            AppendKey(sb, "json");
            SerializeJsonProperty(sb, member.Json);
            sb.Append(',');
        }

        AppendProperty(sb, "kind", member.Kind);
        sb.Append(',');
        AppendProperty(sb, "name", member.Name);
        sb.Append(',');

        if (member.Parameters is { Count: > 0 })
        {
            AppendKey(sb, "parameters");
            sb.Append('[');
            for (int i = 0; i < member.Parameters.Count; i++)
            {
                if (i > 0) sb.Append(',');
                SerializeParameter(sb, member.Parameters[i]);
            }
            sb.Append("],");
        }

        if (member.ReturnType is not null)
        {
            AppendProperty(sb, "returnType", member.ReturnType);
            sb.Append(',');
        }

        AppendProperty(sb, "signature", member.Signature);
        sb.Append('}');
    }

    private static void SerializeParameter(StringBuilder sb, CanonicalParameter param)
    {
        sb.Append('{');
        if (param.DefaultValue is not null)
        {
            AppendProperty(sb, "defaultValue", param.DefaultValue);
            sb.Append(',');
        }
        AppendProperty(sb, "isNullable", param.IsNullable);
        sb.Append(',');
        AppendProperty(sb, "isOptional", param.IsOptional);
        sb.Append(',');
        if (param.Modifier is not null)
        {
            AppendProperty(sb, "modifier", param.Modifier);
            sb.Append(',');
        }
        AppendProperty(sb, "name", param.Name);
        sb.Append(',');
        AppendProperty(sb, "type", param.Type);
        sb.Append('}');
    }

    private static void SerializeJsonContract(StringBuilder sb, CanonicalJsonContract contract)
    {
        sb.Append('{');
        AppendProperty(sb, "contractType", contract.ContractType);
        sb.Append(',');
        AppendKey(sb, "properties");
        sb.Append('[');
        for (int i = 0; i < contract.Properties.Count; i++)
        {
            if (i > 0) sb.Append(',');
            SerializeJsonProperty(sb, contract.Properties[i]);
        }
        sb.Append("],");
        AppendProperty(sb, "useCamelCase", contract.UseCamelCase);
        sb.Append('}');
    }

    private static void SerializeJsonProperty(StringBuilder sb, CanonicalJsonProperty prop)
    {
        sb.Append('{');
        AppendProperty(sb, "clrName", prop.ClrName);
        sb.Append(',');
        AppendProperty(sb, "clrType", prop.ClrType);
        sb.Append(',');
        AppendProperty(sb, "ignored", prop.Ignored);
        sb.Append(',');
        AppendProperty(sb, "jsonName", prop.JsonName);
        sb.Append(',');
        AppendProperty(sb, "jsonType", prop.JsonType);
        sb.Append(',');
        AppendProperty(sb, "nullable", prop.Nullable);
        sb.Append(',');
        AppendProperty(sb, "required", prop.Required);
        sb.Append('}');
    }

    private static void SerializeAIMetadata(StringBuilder sb, CanonicalAIMetadata ai)
    {
        sb.Append('{');
        var first = true;

        if (ai.Category is not null)
        {
            AppendProperty(sb, "category", ai.Category);
            first = false;
        }
        if (ai.Description is not null)
        {
            if (!first) sb.Append(',');
            AppendProperty(sb, "description", ai.Description);
            first = false;
        }
        if (ai.Name is not null)
        {
            if (!first) sb.Append(',');
            AppendProperty(sb, "name", ai.Name);
            first = false;
        }
        if (ai.Role is not null)
        {
            if (!first) sb.Append(',');
            AppendProperty(sb, "role", ai.Role);
            first = false;
        }
        if (ai.Tags is { Count: > 0 })
        {
            if (!first) sb.Append(',');
            AppendKey(sb, "tags");
            SerializeStringArray(sb, ai.Tags);
        }

        sb.Append('}');
    }

    private static void SerializeDocsForHash(StringBuilder sb, CanonicalDocumentation docs)
    {
        // Docs contribute to hash excluding code sample content
        sb.Append('{');
        var first = true;

        if (docs.Parameters is { Count: > 0 })
        {
            AppendKey(sb, "parameters");
            sb.Append('{');
            var pfirst = true;
            foreach (var kvp in docs.Parameters.OrderBy(k => k.Key))
            {
                if (!pfirst) sb.Append(',');
                AppendProperty(sb, kvp.Key, kvp.Value);
                pfirst = false;
            }
            sb.Append('}');
            first = false;
        }
        if (docs.Remarks is not null)
        {
            if (!first) sb.Append(',');
            AppendProperty(sb, "remarks", docs.Remarks);
            first = false;
        }
        if (docs.Returns is not null)
        {
            if (!first) sb.Append(',');
            AppendProperty(sb, "returns", docs.Returns);
            first = false;
        }
        if (docs.Summary is not null)
        {
            if (!first) sb.Append(',');
            AppendProperty(sb, "summary", docs.Summary);
        }
        // Note: Examples content excluded from hash per spec
        // Note: SeeAlso excluded from hash per spec

        sb.Append('}');
    }

    private static void SerializeAttribute(StringBuilder sb, CanonicalAttribute attr)
    {
        sb.Append('{');
        if (attr.Arguments is { Count: > 0 })
        {
            AppendKey(sb, "arguments");
            sb.Append('{');
            var first = true;
            foreach (var kvp in attr.Arguments.OrderBy(k => k.Key))
            {
                if (!first) sb.Append(',');
                AppendProperty(sb, kvp.Key, kvp.Value);
                first = false;
            }
            sb.Append("},");
        }
        AppendProperty(sb, "name", attr.Name);
        sb.Append('}');
    }

    private static void SerializeStringArray(StringBuilder sb, List<string> items)
    {
        sb.Append('[');
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(',');
            AppendStringValue(sb, items[i]);
        }
        sb.Append(']');
    }

    private static void AppendProperty(StringBuilder sb, string key, string value)
    {
        AppendKey(sb, key);
        AppendStringValue(sb, value);
    }

    private static void AppendProperty(StringBuilder sb, string key, bool value)
    {
        AppendKey(sb, key);
        sb.Append(value ? "true" : "false");
    }

    private static void AppendKey(StringBuilder sb, string key)
    {
        sb.Append('"');
        sb.Append(key);
        sb.Append("\":");
    }

    private static void AppendStringValue(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
    }
}
