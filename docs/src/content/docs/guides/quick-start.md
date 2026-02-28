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

## 2. Annotate Your Types (Optional)

```csharp
using ApiContracts;

[AIContract(
    Name = "Customer",
    Description = "A customer entity with contact info.",
    Category = "Domain",
    Role = "entity",
    Tags = "customer,crm")]
public class Customer
{
    public required Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}
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
