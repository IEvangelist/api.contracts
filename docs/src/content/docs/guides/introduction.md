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
- **Documentation-Driven**: Normalized XML docs, code examples, and AI metadata are captured in every schema.

## How It Works

1. Add the `ApiContracts.Generator` source generator to your project.
2. Optionally annotate types with `[ApiContract]` for enhanced AI metadata.
3. Build your project — the generator emits JSON schemas automatically.
4. Consume the schemas in documentation sites, AI agents, or tooling.

## Schema Outputs

| Output | Location | Description |
|---|---|---|
| Root Schema | `schemas/api-schema.json` | JSON Schema definition for assembly schemas |
| Assembly Schema | `ai-skills/apis/reference/<Name>.api-schema.json` | Per-assembly API surface snapshot |
| Vendor Mirror | `<vendor>/apis/reference/<Name>.api-schema.json` | Optional vendor-specific copy |
