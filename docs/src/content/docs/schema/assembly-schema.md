---
title: Assembly Schema
description: Each assembly produces a data-only snapshot of its public API surface.
---

Assembly schemas are emitted to `ai-skills/apis/reference/<AssemblyName>.api-schema.json`.

## Structure

```json
{
  "$schema": "/schemas/api-schema.json",
  "schemaVersion": "1.0.0",
  "package": { "name": "...", "version": "...", "targetFramework": "..." },
  "types": [ ... ],
  "apiHash": "sha256:<hex>",
  "signature": { ... }
}
```

## Types

Each type has: `name`, `fullName`, `namespace`, `kind`, `accessibility`, `ai`, `docs`, `json`, and `members[]`.

### Type Kinds

`class`, `struct`, `record`, `record struct`, `interface`, `enum`, `delegate`

### Members

Each member has `name`, `kind`, `signature`, `returnType`, `parameters[]`, `docs`, `ai`, and `json`.

### JSON Contract

```json
{
  "contractType": "object",
  "useCamelCase": true,
  "properties": [
    {
      "clrName": "FullName",
      "jsonName": "fullName",
      "jsonType": "string",
      "required": true
    }
  ]
}
```
