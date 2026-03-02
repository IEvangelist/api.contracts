---
title: Assembly Schema
description: Each assembly produces a data-only snapshot of its public API surface.
---

Assembly data files are emitted as `{PackageName}.{Version}.json`.

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

Each type has: `name`, `fullName`, `namespace`, `kind`, `accessibility`, `docs`, and `members[]`.

### Type Kinds

`class`, `struct`, `record`, `record struct`, `interface`, `enum`, `delegate`

### Type Flags

| Property | Type | Description |
|---|---|---|
| `isStatic` | `bool` | Static class |
| `isAbstract` | `bool` | Abstract class or interface |
| `isSealed` | `bool` | Sealed class, struct, or enum |
| `isGeneric` | `bool` | Type has generic type parameters |
| `genericParameters` | `array` | Generic parameter names and constraints |
| `interfaces` | `array` | Implemented interface full names |
| `baseType` | `string` | Base type full name (omitted for `System.Object`) |
| `attributes` | `array` | Shape-affecting attributes (e.g., `FlagsAttribute`, `ObsoleteAttribute`) |
| `enumMembers` | `array` | Enum member names, values, and descriptions |

### Members

Each member has `name`, `kind`, `signature`, `returnType`, `parameters[]`, and `docs`.

#### Member Kinds

`property`, `method`, `field`, `event`, `constructor`, `indexer`

#### Member Flags

| Property | Type | Description |
|---|---|---|
| `isStatic` | `bool` | Static member |
| `isAbstract` | `bool` | Abstract member |
| `isVirtual` | `bool` | Virtual member |
| `isOverride` | `bool` | Overrides a base member |
| `isAsync` | `bool` | Async method |
| `isReturnNullable` | `bool` | Return type is nullable |

#### Parameters

Each parameter in `parameters[]` has:

| Property | Type | Description |
|---|---|---|
| `name` | `string` | Parameter name |
| `type` | `string` | Parameter type |
| `isOptional` | `bool` | Has a default value |
| `defaultValue` | `string` | Default value (when optional) |
| `isNullable` | `bool` | Accepts null |
| `modifier` | `string` | `ref`, `out`, `in`, or `params` |

### Documentation

The `docs` object on types and members may contain:

| Property | Type | Description |
|---|---|---|
| `summary` | `string` | One-line description |
| `remarks` | `string` | Extended explanation |
| `returns` | `string` | Return value description |
| `parameters` | `object` | Map of parameter names to descriptions |
| `seeAlso` | `array` | Related type/member references |
