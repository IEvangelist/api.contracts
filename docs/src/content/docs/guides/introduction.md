---
title: Introduction
description: Learn about API Contracts — deterministic, signed, versioned JSON schemas for .NET API surfaces.
---

**API Contracts** is a Roslyn incremental source generator that produces deterministic, signed, versioned JSON schemas describing a .NET assembly's public API surface.

## Why API Contracts?

- **AI-Ready**: Schemas are designed for consumption by AI agents, LLMs, and automated tooling.
- **Deterministic**: Every build produces the same schema for the same API surface. Changes are detectable via `apiHash`.
- **Signed**: Optional RSA-SHA256 signatures ensure schema integrity and provenance.
- **Versioned**: Schema format versioning enables backward-compatible evolution.
- **Documentation-Driven**: Normalized XML docs and code examples are captured in every data file.

## How It Works

1. Add the `ApiContracts.Generator` source generator to your project.
2. Build your project — all public types are captured automatically.
3. Optionally use `[ApiContract(Ignore = true)]` to exclude specific types or members.
4. Consume the data files in documentation sites, AI agents, or tooling.

## Schema Outputs

| Output | Location | Description |
|---|---|---|
| Root Schema | `schemas/api-schema.json` | JSON Schema definition for data files |
| Data File | `{PackageName}.{Version}.json` | Per-assembly API surface snapshot |
| Vendor Mirror | `<vendor>/apis/assets/{PackageName}.{Version}.json` | Optional vendor-specific copy |
