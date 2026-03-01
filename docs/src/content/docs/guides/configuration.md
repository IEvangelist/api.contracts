---
title: Configuration
description: Configure API Contracts via MSBuild properties and assembly attributes.
---

API Contracts supports configuration through MSBuild properties and assembly-level attributes.

## MSBuild Properties

| Property | Default | Description |
|---|---|---|
| `AISchemaEmitStandard` | `true` | Emit schema to standard location |
| `AISchemaEmitVendor` | `false` | Also emit a vendor-specific mirror |
| `AISchemaVendorFolder` | — | Folder name for vendor output |
| `AISchemaSign` | `false` | Sign the schema with RSA-SHA256 |
| `AISchemaSigningPrivateKey` | — | Path to PEM private key file |
| `AISchemaIncludeInternals` | `false` | Include `internal` types and members |

```xml
<PropertyGroup>
  <AISchemaEmitStandard>true</AISchemaEmitStandard>
  <AISchemaIncludeInternals>true</AISchemaIncludeInternals>
  <AISchemaSign>true</AISchemaSign>
  <AISchemaSigningPrivateKey>keys/signing.pem</AISchemaSigningPrivateKey>
</PropertyGroup>
```

## Assembly Attribute

```csharp
[assembly: ApiContractConfig(
    OutputFolder = "ai-skills/apis",
    EmitStandard = true,
    Sign = true,
    SigningKeyId = "pine-2026")]
```

## ApiContractAttribute

Apply `[ApiContract]` at the assembly level to opt-in to schema generation. All public types are included automatically. Use `[ApiContract(Ignore = true)]` on individual types or members to exclude them from the data file:

| Property | Type | Description |
|---|---|---|
| `Ignore` | `bool` | When `true`, excludes the element from the generated data file |
