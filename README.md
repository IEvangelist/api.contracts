# API Contracts

[![CI](https://github.com/IEvangelist/api.contracts/actions/workflows/ci.yml/badge.svg)](https://github.com/IEvangelist/api.contracts/actions/workflows/ci.yml)

Deterministic, signed, versioned JSON schemas that describe a .NET assembly's public API surface — built for AI, tooling, and documentation.

## Overview

**API Contracts** is a Roslyn incremental source generator that walks public symbols in a .NET compilation and emits:

- **Root schema** (`ai-skills/apis/schema.json`) — language definitions, documentation templates, and placeholders.
- **Assembly schemas** (`ai-skills/apis/reference/<AssemblyName>.ai-schema.json`) — data-only snapshots of every public type, member, JSON contract, XML doc, and AI metadata.
- **Optional signed variants** with RSA-SHA256 signature envelopes.

Each schema includes a deterministic `apiHash` (SHA-256 of the canonical API model) so consumers can detect API surface changes without diffing source.

## Projects

| Project | Description |
|---|---|
| `ApiContracts.Abstractions` | Attributes (`AIContractAttribute`, `AIContractConfigAttribute`) and canonical models |
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

### 2. Annotate your types (optional)

```csharp
using ApiContracts;

[AIContract(
    Name = "Customer",
    Description = "A customer entity.",
    Category = "Domain",
    Role = "entity",
    Tags = "customer,crm")]
public class Customer
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
}
```

### 3. Build

The generator emits schema code during compilation. The schema captures all public types, members, signatures, XML docs, JSON serialization contracts, and AI metadata.

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
[assembly: AIContractConfig(
    OutputFolder = "ai-skills/apis",
    EmitStandard = true,
    Sign = true,
    SigningKeyId = "my-key-2026")]
```

## Schema Format

### Assembly Schema

```json
{
  "schemaVersion": "1.0.0",
  "rootSchema": "../../schema.json",
  "package": {
    "name": "MyAssembly",
    "version": "1.0.0",
    "targetFramework": "net10.0"
  },
  "types": [ ... ],
  "apiHash": "sha256:<hex>"
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

// Compute hash
string hash = SchemaVerifier.ComputeApiHash(canonicalJson);
```

## SKILL.md

See [SKILL.md](SKILL.md) for the agentic AI skill definition that teaches AI agents how to consume, interpret, and act on emitted schemas.

## License

[MIT](LICENSE)