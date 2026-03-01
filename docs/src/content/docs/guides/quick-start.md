---
title: Quick Start
description: Get up and running with API Contracts in under 5 minutes.
---

## 1. Add NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="ApiContracts.Abstractions" />
  <PackageReference Include="ApiContracts.Generator"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## 2. Mark Your Assembly

All public types are included automatically. Use `[ApiContract(Ignore = true)]` to exclude specific types or members:

```csharp
using ApiContracts;

// All public types are emitted to the data file.
// To exclude a type:
[ApiContract(Ignore = true)]
public class InternalHelper { }
```

## 3. Build

```bash
dotnet build
```

The generator automatically walks all public types and members, extracts XML documentation, models System.Text.Json serialization behavior, computes a deterministic `apiHash`, and emits schema as embedded source.

## 4. Access the Schema

```csharp
using ApiContracts.Generated;

var json = EmbeddedSchemas.MyAssemblySchema;
```

## 5. Verify (Optional)

```csharp
using ApiContracts.Verification;

var result = SchemaVerifier.ValidateSchema(json);
Console.WriteLine($"Valid: {result.IsValid}, Hash: {result.ApiHash}");
```
