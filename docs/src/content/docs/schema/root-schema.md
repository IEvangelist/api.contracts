---
title: Root Schema
description: The api-schema.json JSON Schema definition validates assembly schemas emitted by the generator.
---

The root schema (`/schemas/api-schema.json`) is a **JSON Schema** definition that validates the structure of every assembly schema emitted by the generator. Assembly schemas reference it via the standard `$schema` keyword.

## Structure

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "/schemas/api-schema.json",
  "title": "API Contracts Assembly Schema",
  "description": "Validates assembly schemas produced by ApiContracts.Generator.",
  "type": "object",
  "required": ["schemaVersion", "package", "types", "apiHash"],
  "properties": {
    "schemaVersion": { "type": "string" },
    "package": { "$ref": "#/$defs/package" },
    "types": { "type": "array", "items": { "$ref": "#/$defs/type" } },
    "apiHash": { "type": "string" },
    "signature": { "$ref": "#/$defs/signature" }
  }
}
```

## Usage

Assembly schemas reference the root schema using the `$schema` keyword:

```json
{
  "$schema": "/schemas/api-schema.json",
  "schemaVersion": "1.0.0",
  "package": { "name": "MyApi", "version": "1.0.0.0", "targetFramework": "net10.0" },
  "types": [ ... ],
  "apiHash": "sha256:<hex>"
}
```

## Key Definitions

| Definition | Description |
|---|---|
| `package` | Assembly name, version, and target framework |
| `type` | A public type with name, kind, docs, and members |
| `member` | A type member (property, method, event, field, constructor) |
| `signature` | Optional RSA-SHA256 signature for integrity verification |

## Placeholders

| Placeholder | Resolves To |
|---|---|
| `{type.fullName}` | Fully qualified type name |
| `{type.namespace}` | Namespace |
| `{member.signature}` | Full member signature |
| `{package.name}` | Assembly name |
| `{apiHash}` | Deterministic API hash |
