---
title: Root Schema
description: The root schema.json defines language definitions, documentation templates, and placeholders.
---

The root schema (`ai-skills/apis/schema.json`) provides shared definitions used by all assembly schemas.

## Structure

```json
{
  "schemaVersion": "1.0.0",
  "generator": "ApiContracts.Generator",
  "languages": {
    "default": "csharp",
    "available": [...]
  },
  "documentation": {
    "templates": { ... },
    "placeholders": { ... }
  }
}
```

## Languages

The `languages` section defines available programming languages for polyglot code rendering. Each entry has `id`, `displayName`, `extension`, and `syntaxAlias`.

## Templates

| Key | Template | Description |
|---|---|---|
| `namespaceIndex` | `NamespaceIndex.mdx` | Namespace listing page |
| `type` | `TypePage.mdx` | Individual type page |
| `member` | `MemberPage.mdx` | Individual member page |
| `enum` | `EnumPage.mdx` | Enum type page |
| `jsonContract` | `JsonContractPage.mdx` | JSON contract documentation |

## Placeholders

| Placeholder | Resolves To |
|---|---|
| `{type.ai.name}` | AI display name |
| `{type.fullName}` | Fully qualified type name |
| `{type.namespace}` | Namespace |
| `{member.signature}` | Full member signature |
| `{package.name}` | Assembly name |
| `{apiHash}` | Deterministic API hash |
