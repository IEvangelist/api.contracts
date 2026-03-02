---
title: Canonical Hashing
description: How API Contracts computes deterministic hashes of the API surface.
---

The `apiHash` provides a deterministic fingerprint of the API surface.

## Included in Hash

- Public types and members with signatures
- Parameter names, types, nullability
- Shape-affecting attributes (`[Obsolete]`, `[Flags]`, `[JsonDerivedType]`)
- JSON serialization metadata
- `ApiContractAttribute` metadata
- XML documentation text (summary, remarks, params, returns)

## Excluded from Hash

- File paths, timestamps, build metadata
- Code sample content
- Ordering differences (canonical sorting applied)
- Whitespace and formatting

## Process

1. **Build** canonical model from public types/members
2. **Sort** by namespace → type → member kind → name
3. **Serialize** to UTF-8 JSON with sorted properties, no whitespace
4. **Hash** with SHA-256
5. **Format** as `sha256:<lowercase-hex>`

## Verification

```csharp
using ApiContracts.Verification;

// Validate schema structure and hash format
var result = SchemaVerifier.ValidateSchema(schemaJson);

// Compute a hash from canonical JSON
var hash = SchemaVerifier.ComputeApiHash(canonicalJson);

// Verify the declared apiHash matches the types in the schema
var hashResult = SchemaVerifier.VerifyApiHash(schemaJson);
if (!hashResult.IsMatch)
    Console.WriteLine($"Hash mismatch: declared={hashResult.DeclaredHash}, computed={hashResult.ComputedHash}");
```
