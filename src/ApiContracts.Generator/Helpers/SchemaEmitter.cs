// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Emits the assembly schema JSON with deterministic formatting.
/// The output contains only public types/members with parsed docs and is
/// ordered so that two identical API surfaces always produce identical JSON.
/// </summary>
internal static class SchemaEmitter
{
    public static string EmitAssemblySchema(
        string assemblyName,
        string assemblyVersion,
        string targetFramework,
        List<CanonicalType> types,
        string apiHash,
        AssemblyConfig config,
        string? signatureValue = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"$schema\": \"https://ievangelist.github.io/api.contracts/schemas/api-schema.json\",");
        sb.AppendLine($"  \"schemaVersion\": \"1.0.0\",");
        sb.AppendLine($"  \"package\": {{");
        sb.AppendLine($"    \"name\": \"{EscapeJson(assemblyName)}\",");
        sb.AppendLine($"    \"version\": \"{EscapeJson(assemblyVersion)}\",");
        sb.AppendLine($"    \"targetFramework\": \"{EscapeJson(targetFramework)}\"");
        sb.AppendLine($"  }},");
        sb.AppendLine($"  \"apiHash\": \"{EscapeJson(apiHash)}\",");

        // Signature envelope (optional)
        if (signatureValue is not null && config.SigningKeyId is not null)
        {
            sb.AppendLine($"  \"signature\": {{");
            sb.AppendLine($"    \"algorithm\": \"RSA-SHA256\",");
            sb.AppendLine($"    \"publicKeyId\": \"{EscapeJson(config.SigningKeyId)}\",");
            sb.AppendLine($"    \"value\": \"{EscapeJson(signatureValue)}\"");
            sb.AppendLine($"  }},");
        }

        // Types — deterministically sorted
        sb.AppendLine($"  \"types\": [");

        var sortedTypes = types
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Name)
            .ToList();

        for (int i = 0; i < sortedTypes.Count; i++)
        {
            EmitType(sb, sortedTypes[i], "    ");
            if (i < sortedTypes.Count - 1) sb.AppendLine(",");
            else sb.AppendLine();
        }

        sb.AppendLine($"  ]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void EmitType(StringBuilder sb, CanonicalType type, string indent)
    {
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  \"name\": \"{EscapeJson(type.Name)}\",");
        sb.AppendLine($"{indent}  \"fullName\": \"{EscapeJson(type.FullName)}\",");
        sb.AppendLine($"{indent}  \"namespace\": \"{EscapeJson(type.Namespace)}\",");
        sb.AppendLine($"{indent}  \"kind\": \"{EscapeJson(type.Kind)}\",");

        // Collect remaining properties so we can avoid trailing commas
        var parts = new List<string>();

        // Boolean flags — only emit when true
        if (type.IsAbstract) parts.Add($"{indent}  \"isAbstract\": true");
        if (type.IsSealed) parts.Add($"{indent}  \"isSealed\": true");
        if (type.IsStatic) parts.Add($"{indent}  \"isStatic\": true");
        if (type.IsGeneric) parts.Add($"{indent}  \"isGeneric\": true");

        // Generic parameters
        if (type.GenericParameters is { Count: > 0 })
        {
            var gpSb = new StringBuilder();
            gpSb.Append($"{indent}  \"genericParameters\": [");
            for (int i = 0; i < type.GenericParameters.Count; i++)
            {
                var gp = type.GenericParameters[i];
                gpSb.Append($"{{\"name\": \"{EscapeJson(gp.Name)}\"");
                if (gp.Constraints.Count > 0)
                {
                    gpSb.Append(", \"constraints\": [");
                    gpSb.Append(string.Join(", ", gp.Constraints.Select(c => $"\"{EscapeJson(c)}\"")));
                    gpSb.Append(']');
                }
                gpSb.Append('}');
                if (i < type.GenericParameters.Count - 1) gpSb.Append(", ");
            }
            gpSb.Append(']');
            parts.Add(gpSb.ToString());
        }

        // Base type
        if (type.BaseType is not null)
        {
            parts.Add($"{indent}  \"baseType\": \"{EscapeJson(type.BaseType)}\"");
        }

        // Interfaces
        if (type.Interfaces.Count > 0)
        {
            var ifaces = string.Join(", ", type.Interfaces.Select(i => $"\"{EscapeJson(i)}\""));
            parts.Add($"{indent}  \"interfaces\": [{ifaces}]");
        }

        // Docs
        if (type.Docs is not null)
        {
            parts.Add(BuildDocumentation(type.Docs, indent + "  "));
        }

        // JSON contract
        if (type.Json is not null)
        {
            parts.Add(BuildJsonContract(type.Json, indent + "  "));
        }

        // Enum members
        if (type.EnumMembers is { Count: > 0 })
        {
            var emSb = new StringBuilder();
            emSb.AppendLine($"{indent}  \"enumMembers\": [");
            for (int i = 0; i < type.EnumMembers.Count; i++)
            {
                var em = type.EnumMembers[i];
                emSb.Append($"{indent}    {{\"name\": \"{EscapeJson(em.Name)}\", \"value\": {em.Value}");
                if (em.Description is not null)
                {
                    emSb.Append($", \"description\": \"{EscapeJson(em.Description)}\"");
                }
                emSb.Append('}');
                if (i < type.EnumMembers.Count - 1) emSb.AppendLine(",");
                else emSb.AppendLine();
            }
            emSb.Append($"{indent}  ]");
            parts.Add(emSb.ToString());
        }

        // Members — deterministically sorted
        var membersSb = new StringBuilder();
        membersSb.AppendLine($"{indent}  \"members\": [");
        var sortedMembers = type.Members
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.Name)
            .ToList();

        for (int i = 0; i < sortedMembers.Count; i++)
        {
            EmitMember(membersSb, sortedMembers[i], indent + "    ");
            if (i < sortedMembers.Count - 1) membersSb.AppendLine(",");
            else membersSb.AppendLine();
        }
        membersSb.Append($"{indent}  ]");
        parts.Add(membersSb.ToString());

        // Write accessibility as last simple prop before the parts
        sb.Append($"{indent}  \"accessibility\": \"{EscapeJson(type.Accessibility)}\"");

        foreach (var part in parts)
        {
            sb.AppendLine(",");
            sb.Append(part);
        }

        sb.AppendLine();
        sb.Append($"{indent}}}");
    }

    private static void EmitMember(StringBuilder sb, CanonicalMember member, string indent)
    {
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  \"name\": \"{EscapeJson(member.Name)}\",");
        sb.AppendLine($"{indent}  \"kind\": \"{EscapeJson(member.Kind)}\",");
        sb.Append($"{indent}  \"signature\": \"{EscapeJson(member.Signature)}\"");

        var parts = new List<string>();

        if (member.ReturnType is not null)
        {
            parts.Add($"{indent}  \"returnType\": \"{EscapeJson(member.ReturnType)}\"");
            if (member.IsReturnNullable)
            {
                parts.Add($"{indent}  \"isReturnNullable\": true");
            }
        }

        if (member.IsStatic) parts.Add($"{indent}  \"isStatic\": true");
        if (member.IsAbstract) parts.Add($"{indent}  \"isAbstract\": true");
        if (member.IsVirtual) parts.Add($"{indent}  \"isVirtual\": true");
        if (member.IsOverride) parts.Add($"{indent}  \"isOverride\": true");
        if (member.IsAsync) parts.Add($"{indent}  \"isAsync\": true");

        // Parameters
        if (member.Parameters is { Count: > 0 })
        {
            var pSb = new StringBuilder();
            pSb.AppendLine($"{indent}  \"parameters\": [");
            for (int i = 0; i < member.Parameters.Count; i++)
            {
                var p = member.Parameters[i];
                pSb.Append($"{indent}    {{\"name\": \"{EscapeJson(p.Name)}\", \"type\": \"{EscapeJson(p.Type)}\"");
                if (p.IsNullable) pSb.Append(", \"isNullable\": true");
                if (p.IsOptional)
                {
                    pSb.Append(", \"isOptional\": true");
                    if (p.DefaultValue is not null) pSb.Append($", \"defaultValue\": \"{EscapeJson(p.DefaultValue)}\"");
                }
                if (p.Modifier is not null) pSb.Append($", \"modifier\": \"{EscapeJson(p.Modifier)}\"");
                pSb.Append('}');
                if (i < member.Parameters.Count - 1) pSb.AppendLine(",");
                else pSb.AppendLine();
            }
            pSb.Append($"{indent}  ]");
            parts.Add(pSb.ToString());
        }

        // Docs
        if (member.Docs is not null)
        {
            parts.Add(BuildDocumentation(member.Docs, indent + "  "));
        }

        foreach (var part in parts)
        {
            sb.AppendLine(",");
            sb.Append(part);
        }

        sb.AppendLine();
        sb.Append($"{indent}}}");
    }

    private static string BuildDocumentation(CanonicalDocumentation docs, string indent)
    {
        var docParts = new List<string>();

        if (docs.Summary is not null) docParts.Add($"\"summary\": \"{EscapeJson(docs.Summary)}\"");
        if (docs.Remarks is not null) docParts.Add($"\"remarks\": \"{EscapeJson(docs.Remarks)}\"");
        if (docs.Returns is not null) docParts.Add($"\"returns\": \"{EscapeJson(docs.Returns)}\"");

        if (docs.Parameters is { Count: > 0 })
        {
            var paramParts = docs.Parameters
                .OrderBy(p => p.Key)
                .Select(p => $"\"{EscapeJson(p.Key)}\": \"{EscapeJson(p.Value)}\"");
            docParts.Add($"\"parameters\": {{{string.Join(", ", paramParts)}}}");
        }

        if (docs.SeeAlso is { Count: > 0 })
        {
            docParts.Add($"\"seeAlso\": [{string.Join(", ", docs.SeeAlso.Select(s => $"\"{EscapeJson(s)}\""))}]");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{indent}\"docs\": {{");
        sb.AppendLine($"{indent}  {string.Join($",\n{indent}  ", docParts)}");
        sb.Append($"{indent}}}");
        return sb.ToString();
    }

    private static string BuildJsonContract(CanonicalJsonContract contract, string indent)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{indent}\"json\": {{");
        sb.AppendLine($"{indent}  \"contractType\": \"{EscapeJson(contract.ContractType)}\",");
        sb.AppendLine($"{indent}  \"useCamelCase\": {(contract.UseCamelCase ? "true" : "false")},");
        sb.AppendLine($"{indent}  \"properties\": [");

        for (int i = 0; i < contract.Properties.Count; i++)
        {
            var p = contract.Properties[i];
            sb.Append($"{indent}    {{\"clrName\": \"{EscapeJson(p.ClrName)}\", \"jsonName\": \"{EscapeJson(p.JsonName)}\", \"jsonType\": \"{EscapeJson(p.JsonType)}\", \"clrType\": \"{EscapeJson(p.ClrType)}\"");
            if (p.Ignored) sb.Append(", \"ignored\": true");
            if (p.Nullable) sb.Append(", \"nullable\": true");
            if (p.Required) sb.Append(", \"required\": true");
            if (p.Description is not null) sb.Append($", \"description\": \"{EscapeJson(p.Description)}\"");
            sb.Append('}');
            if (i < contract.Properties.Count - 1) sb.AppendLine(",");
            else sb.AppendLine();
        }

        sb.AppendLine($"{indent}  ]");
        sb.Append($"{indent}}}");
        return sb.ToString();
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
