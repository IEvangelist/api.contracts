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

Apply `[ApiContract]` to types, methods, properties, or parameters:

| Property | Type | Description |
|---|---|---|
| `Name` | `string?` | Override display name |
| `Description` | `string?` | AI-oriented description |
| `Category` | `string?` | Grouping category |
| `Role` | `string?` | Semantic role |
| `Tags` | `string?` | Comma-separated tags |
| `Exclude` | `bool` | Exclude from schema |
