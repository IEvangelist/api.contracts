// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace ApiContracts.Generator.Helpers;

/// <summary>
/// Emits the assembly schema JSON with deterministic formatting.
/// </summary>
internal static class SchemaEmitter
{
    public static string EmitAssemblySchema(
        string assemblyName,
        string assemblyVersion,
        string targetFramework,
        List<CanonicalType> types,
        string apiHash,
        AssemblyConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"schemaVersion\": \"1.0.0\",");
        sb.AppendLine($"  \"rootSchema\": \"../../schema.json\",");
        sb.AppendLine($"  \"package\": {{");
        sb.AppendLine($"    \"name\": \"{EscapeJson(assemblyName)}\",");
        sb.AppendLine($"    \"version\": \"{EscapeJson(assemblyVersion)}\",");
        sb.AppendLine($"    \"targetFramework\": \"{EscapeJson(targetFramework)}\"");
        sb.AppendLine($"  }},");

        // Types
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

        sb.AppendLine($"  ],");
        sb.AppendLine($"  \"apiHash\": \"{EscapeJson(apiHash)}\"");
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
        sb.AppendLine($"{indent}  \"accessibility\": \"{EscapeJson(type.Accessibility)}\",");

        // Boolean flags
        if (type.IsAbstract) sb.AppendLine($"{indent}  \"isAbstract\": true,");
        if (type.IsSealed) sb.AppendLine($"{indent}  \"isSealed\": true,");
        if (type.IsStatic) sb.AppendLine($"{indent}  \"isStatic\": true,");
        if (type.IsGeneric)
        {
            sb.AppendLine($"{indent}  \"isGeneric\": true,");
            if (type.GenericParameters is { Count: > 0 })
            {
                sb.AppendLine($"{indent}  \"genericParameters\": [");
                for (int i = 0; i < type.GenericParameters.Count; i++)
                {
                    var gp = type.GenericParameters[i];
                    sb.Append($"{indent}    {{\"name\": \"{EscapeJson(gp.Name)}\"");
                    if (gp.Constraints.Count > 0)
                    {
                        sb.Append(", \"constraints\": [");
                        sb.Append(string.Join(", ", gp.Constraints.Select(c => $"\"{EscapeJson(c)}\"")));
                        sb.Append(']');
                    }
                    sb.Append('}');
                    if (i < type.GenericParameters.Count - 1) sb.AppendLine(",");
                    else sb.AppendLine();
                }
                sb.AppendLine($"{indent}  ],");
            }
        }

        // Base type
        if (type.BaseType is not null)
        {
            sb.AppendLine($"{indent}  \"baseType\": \"{EscapeJson(type.BaseType)}\",");
        }

        // Interfaces
        if (type.Interfaces.Count > 0)
        {
            sb.Append($"{indent}  \"interfaces\": [");
            sb.Append(string.Join(", ", type.Interfaces.Select(i => $"\"{EscapeJson(i)}\"")));
            sb.AppendLine("],");
        }

        // AI metadata
        if (type.AI is not null)
        {
            sb.AppendLine($"{indent}  \"ai\": {{");
            var aiParts = new List<string>();
            if (type.AI.Name is not null) aiParts.Add($"\"name\": \"{EscapeJson(type.AI.Name)}\"");
            if (type.AI.Description is not null) aiParts.Add($"\"description\": \"{EscapeJson(type.AI.Description)}\"");
            if (type.AI.Category is not null) aiParts.Add($"\"category\": \"{EscapeJson(type.AI.Category)}\"");
            if (type.AI.Role is not null) aiParts.Add($"\"role\": \"{EscapeJson(type.AI.Role)}\"");
            if (type.AI.Tags is { Count: > 0 })
            {
                aiParts.Add($"\"tags\": [{string.Join(", ", type.AI.Tags.Select(t => $"\"{EscapeJson(t)}\""))}]");
            }
            sb.AppendLine($"{indent}    {string.Join($",\n{indent}    ", aiParts)}");
            sb.AppendLine($"{indent}  }},");
        }

        // Docs
        if (type.Docs is not null)
        {
            EmitDocumentation(sb, type.Docs, indent + "  ");
        }

        // JSON contract
        if (type.Json is not null)
        {
            EmitJsonContract(sb, type.Json, indent + "  ");
        }

        // Enum members
        if (type.EnumMembers is { Count: > 0 })
        {
            sb.AppendLine($"{indent}  \"enumMembers\": [");
            for (int i = 0; i < type.EnumMembers.Count; i++)
            {
                var em = type.EnumMembers[i];
                sb.Append($"{indent}    {{\"name\": \"{EscapeJson(em.Name)}\", \"value\": {em.Value}");
                if (em.Description is not null)
                {
                    sb.Append($", \"description\": \"{EscapeJson(em.Description)}\"");
                }
                sb.Append('}');
                if (i < type.EnumMembers.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine($"{indent}  ],");
        }

        // Attributes
        if (type.Attributes is { Count: > 0 })
        {
            EmitAttributes(sb, type.Attributes, indent + "  ");
        }

        // Members
        sb.AppendLine($"{indent}  \"members\": [");
        var sortedMembers = type.Members
            .OrderBy(m => m.Kind)
            .ThenBy(m => m.Name)
            .ToList();

        for (int i = 0; i < sortedMembers.Count; i++)
        {
            EmitMember(sb, sortedMembers[i], indent + "    ");
            if (i < sortedMembers.Count - 1) sb.AppendLine(",");
            else sb.AppendLine();
        }
        sb.AppendLine($"{indent}  ]");
        sb.Append($"{indent}}}");
    }

    private static void EmitMember(StringBuilder sb, CanonicalMember member, string indent)
    {
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}  \"name\": \"{EscapeJson(member.Name)}\",");
        sb.AppendLine($"{indent}  \"kind\": \"{EscapeJson(member.Kind)}\",");
        sb.AppendLine($"{indent}  \"accessibility\": \"{EscapeJson(member.Accessibility)}\",");
        sb.AppendLine($"{indent}  \"signature\": \"{EscapeJson(member.Signature)}\",");

        if (member.ReturnType is not null)
        {
            sb.AppendLine($"{indent}  \"returnType\": \"{EscapeJson(member.ReturnType)}\",");
            if (member.IsReturnNullable)
            {
                sb.AppendLine($"{indent}  \"isReturnNullable\": true,");
            }
        }

        if (member.IsStatic) sb.AppendLine($"{indent}  \"isStatic\": true,");
        if (member.IsAbstract) sb.AppendLine($"{indent}  \"isAbstract\": true,");
        if (member.IsVirtual) sb.AppendLine($"{indent}  \"isVirtual\": true,");
        if (member.IsOverride) sb.AppendLine($"{indent}  \"isOverride\": true,");
        if (member.IsAsync) sb.AppendLine($"{indent}  \"isAsync\": true,");

        // Parameters
        if (member.Parameters is { Count: > 0 })
        {
            sb.AppendLine($"{indent}  \"parameters\": [");
            for (int i = 0; i < member.Parameters.Count; i++)
            {
                var p = member.Parameters[i];
                sb.Append($"{indent}    {{\"name\": \"{EscapeJson(p.Name)}\", \"type\": \"{EscapeJson(p.Type)}\"");
                if (p.IsNullable) sb.Append(", \"isNullable\": true");
                if (p.IsOptional)
                {
                    sb.Append(", \"isOptional\": true");
                    if (p.DefaultValue is not null) sb.Append($", \"defaultValue\": \"{EscapeJson(p.DefaultValue)}\"");
                }
                if (p.Modifier is not null) sb.Append($", \"modifier\": \"{EscapeJson(p.Modifier)}\"");
                sb.Append('}');
                if (i < member.Parameters.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine($"{indent}  ],");
        }

        // AI metadata
        if (member.AI is not null)
        {
            sb.AppendLine($"{indent}  \"ai\": {{");
            var aiParts = new List<string>();
            if (member.AI.Name is not null) aiParts.Add($"\"name\": \"{EscapeJson(member.AI.Name)}\"");
            if (member.AI.Description is not null) aiParts.Add($"\"description\": \"{EscapeJson(member.AI.Description)}\"");
            if (member.AI.Category is not null) aiParts.Add($"\"category\": \"{EscapeJson(member.AI.Category)}\"");
            if (member.AI.Role is not null) aiParts.Add($"\"role\": \"{EscapeJson(member.AI.Role)}\"");
            sb.AppendLine($"{indent}    {string.Join($",\n{indent}    ", aiParts)}");
            sb.AppendLine($"{indent}  }},");
        }

        // Docs
        if (member.Docs is not null)
        {
            EmitDocumentation(sb, member.Docs, indent + "  ");
        }

        // JSON property metadata
        if (member.Json is not null)
        {
            sb.AppendLine($"{indent}  \"json\": {{");
            sb.AppendLine($"{indent}    \"clrName\": \"{EscapeJson(member.Json.ClrName)}\",");
            sb.AppendLine($"{indent}    \"jsonName\": \"{EscapeJson(member.Json.JsonName)}\",");
            sb.AppendLine($"{indent}    \"jsonType\": \"{EscapeJson(member.Json.JsonType)}\",");
            sb.AppendLine($"{indent}    \"clrType\": \"{EscapeJson(member.Json.ClrType)}\",");
            if (member.Json.Ignored) sb.AppendLine($"{indent}    \"ignored\": true,");
            if (member.Json.Nullable) sb.AppendLine($"{indent}    \"nullable\": true,");
            if (member.Json.Required) sb.AppendLine($"{indent}    \"required\": true,");
            if (member.Json.Description is not null)
                sb.AppendLine($"{indent}    \"description\": \"{EscapeJson(member.Json.Description)}\",");
            // Remove trailing comma from last property
            sb.AppendLine($"{indent}  }},");
        }

        // Remove trailing comma by checking last character
        sb.AppendLine($"{indent}  \"_\": true");
        sb.Append($"{indent}}}");
    }

    private static void EmitDocumentation(StringBuilder sb, CanonicalDocumentation docs, string indent)
    {
        sb.AppendLine($"{indent}\"docs\": {{");
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

        if (docs.Examples is { Count: > 0 })
        {
            var exampleParts = new List<string>();
            foreach (var example in docs.Examples)
            {
                var parts = new List<string> { $"\"language\": \"{EscapeJson(example.Language)}\"" };
                if (example.Region is not null) parts.Add($"\"region\": \"{EscapeJson(example.Region)}\"");
                parts.Add($"\"code\": \"{EscapeJson(example.Code)}\"");
                if (example.Description is not null) parts.Add($"\"description\": \"{EscapeJson(example.Description)}\"");
                exampleParts.Add($"{{{string.Join(", ", parts)}}}");
            }
            docParts.Add($"\"examples\": [{string.Join(", ", exampleParts)}]");
        }

        if (docs.SeeAlso is { Count: > 0 })
        {
            docParts.Add($"\"seeAlso\": [{string.Join(", ", docs.SeeAlso.Select(s => $"\"{EscapeJson(s)}\""))}]");
        }

        sb.AppendLine($"{indent}  {string.Join($",\n{indent}  ", docParts)}");
        sb.AppendLine($"{indent}}},");
    }

    private static void EmitJsonContract(StringBuilder sb, CanonicalJsonContract contract, string indent)
    {
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
        sb.AppendLine($"{indent}}},");
    }

    private static void EmitAttributes(StringBuilder sb, List<CanonicalAttribute> attrs, string indent)
    {
        sb.AppendLine($"{indent}\"attributes\": [");
        for (int i = 0; i < attrs.Count; i++)
        {
            var a = attrs[i];
            sb.Append($"{indent}  {{\"name\": \"{EscapeJson(a.Name)}\"");
            if (a.Arguments is { Count: > 0 })
            {
                sb.Append(", \"arguments\": {");
                sb.Append(string.Join(", ", a.Arguments.Select(kvp => $"\"{EscapeJson(kvp.Key)}\": \"{EscapeJson(kvp.Value)}\"")));
                sb.Append('}');
            }
            sb.Append('}');
            if (i < attrs.Count - 1) sb.AppendLine(",");
            else sb.AppendLine();
        }
        sb.AppendLine($"{indent}],");
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
