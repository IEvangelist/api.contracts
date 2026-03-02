# API Contracts

[![CI](https://github.com/IEvangelist/api.contracts/actions/workflows/ci.yml/badge.svg)](https://github.com/IEvangelist/api.contracts/actions/workflows/ci.yml)
[![Docs](https://github.com/IEvangelist/api.contracts/actions/workflows/deploy-docs.yml/badge.svg)](https://ievangelist.github.io/api.contracts/)

Deterministic, signed, versioned JSON schemas that describe a .NET assembly's public API surface — built for AI, tooling, and documentation.

## Overview

**API Contracts** is a Roslyn incremental source generator that walks public symbols in a .NET compilation and emits:

- **Root schema** (`schemas/api-schema.json`) — JSON Schema definition that validates the structure of assembly data files.
- **Assembly data files** (`{PackageName}.{Version}.json`) — data-only snapshots of every public type, member, and XML documentation.
- **Optional signed variants** with RSA-SHA256 signature envelopes.

Each schema includes a deterministic `apiHash` (SHA-256 of the canonical API model) so consumers can detect API surface changes without diffing source.

## Projects

| Project | Description |
|---|---|
| `ApiContracts.Abstractions` | Attributes (`ApiContractAttribute`, `ApiContractConfigAttribute`) and canonical models |
| `ApiContracts.Generator` | Roslyn incremental source generator |
| `ApiContracts.Verification` | SDK for hash computation and RSA-SHA256 signature verification |
| `SampleApi` | Example project demonstrating generator usage |

## Quick Start

### 1. Add the NuGet packages

```xml
<PackageReference Include="ApiContracts.Abstractions" />
<PackageReference Include="ApiContracts.Generator"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

### 2. Mark your assembly

All public types are included automatically. Use `[ApiContract(Ignore = true)]` to exclude specific types or members:

```csharp
using ApiContracts;

// All public types in this assembly are emitted to the data file.
// To exclude a type:
[ApiContract(Ignore = true)]
public class InternalHelper { }
```

### 3. Build

The generator emits data file code during compilation. The data file captures all public types, members, signatures, and XML docs.

## Configuration

### MSBuild Properties

| Property | Default | Description |
|---|---|---|
| `AISchemaEmitStandard` | `true` | Emit schema to standard output location |
| `AISchemaEmitVendor` | `false` | Emit vendor-specific mirror |
| `AISchemaVendorFolder` | — | Vendor output folder name |
| `AISchemaSign` | `false` | Sign schema with RSA-SHA256 |
| `AISchemaSigningPrivateKey` | — | Path to PEM private key |
| `AISchemaIncludeInternals` | `false` | Include internal types/members |

### Assembly Attribute

```csharp
[assembly: ApiContractConfig(
    OutputFolder = "ai-skills/apis",
    EmitStandard = true,
    Sign = true,
    SigningKeyId = "my-key-2026")]
```

### Enabling Signing

To sign your schemas with RSA-SHA256:

1. Generate an RSA key pair:

   ```bash
   openssl genrsa -out private.pem 2048
   openssl rsa -in private.pem -pubout -out public.pem
   ```

2. Configure your project:

   ```xml
   <PropertyGroup>
     <AISchemaSign>true</AISchemaSign>
     <AISchemaSigningPrivateKey>path/to/private.pem</AISchemaSigningPrivateKey>
   </PropertyGroup>
   ```

3. The emitted schema will include a `signature` envelope that consumers can verify with the public key.

### Vendor Mirrors

To emit vendor-specific copies of your schema:

```xml
<PropertyGroup>
  <AISchemaEmitVendor>true</AISchemaEmitVendor>
  <AISchemaVendorFolder>my-vendor</AISchemaVendorFolder>
</PropertyGroup>
```

This writes a mirror to `<VendorFolder>/apis/reference/<AssemblyName>.json`.

## Schema Format

### Assembly Schema

```json
{
  "$schema": "https://ievangelist.github.io/api.contracts/schemas/api-schema.json",
  "schemaVersion": "1.0.0",
  "package": {
    "name": "MyAssembly",
    "version": "1.0.0",
    "targetFramework": "net10.0"
  },
  "types": [ ... ],
  "apiHash": "sha256:<hex>",
  "signature": {
    "algorithm": "RSA-SHA256",
    "publicKeyId": "pine-2026",
    "value": "<base64>"
  }
}
```

### Canonical Hashing

The `apiHash` is computed by:

1. Building a canonical model of all public types and members
2. Sorting by namespace → type → member
3. Serializing to UTF-8 JSON with sorted properties and no whitespace
4. Computing SHA-256

Excluded from hash: file paths, timestamps, build metadata, code sample content, ordering differences, whitespace.

### Signature Envelope

```json
{
  "signature": {
    "algorithm": "RSA-SHA256",
    "publicKeyId": "pine-2026",
    "value": "<base64>"
  }
}
```

## Verification SDK

```csharp
using ApiContracts.Verification;

// Validate schema structure
var result = SchemaVerifier.ValidateSchema(schemaJson);
if (result.IsValid)
    Console.WriteLine($"Hash: {result.ApiHash}, Version: {result.SchemaVersion}");

// Verify signature
bool valid = SchemaVerifier.VerifySignature(data, signatureBase64, publicKeyPem);

// Sign data
string signature = SchemaVerifier.SignData(canonicalJson, privateKeyPem);

// Compute hash
string hash = SchemaVerifier.ComputeApiHash(canonicalJson);
```

## Generating Documentation

The project includes an [Astro/Starlight](https://starlight.astro.build/) documentation site that consumes the emitted schemas.

### Running locally

```bash
cd docs
npm install
npm run dev
```

### How it works

1. The schema loader plugin (`docs/src/plugins/schema-loader.mjs`) loads `schema.json` and all `*.json` data files at build time
2. It builds type and namespace indexes for fast lookup
3. Astro components (`TypeSignature`, `MemberList`, `Examples`) render API documentation from the schema data
4. Polyglot code samples are shown in tabbed views with copy buttons

The documentation site is deployed to [GitHub Pages](https://ievangelist.github.io/api.contracts/) automatically on changes to `docs/` or `ai-skills/`.

## SKILL.md

See [SKILL.md](SKILL.md) for the agentic AI skill definition that teaches AI agents how to consume, interpret, and act on emitted schemas.

## License

[MIT](LICENSE)
