---
title: API Reference
description: Auto-generated API reference from schema files.
---

This section contains auto-generated API documentation from the emitted `*.api-schema.json` files.

## Schema Components

The documentation site includes reusable Astro components for rendering API schemas:

### TypeSignature

Displays a type's full signature with generics, constraints, inheritance chain, and modifier badges.

### MemberList

Renders all members of a type grouped by kind (constructors, properties, methods, fields, events). Each member shows its signature, modifiers (static, abstract, virtual, async), and documentation summary.

### JsonContract

Displays the System.Text.Json serialization contract for a type — property names, types, required/nullable/ignored flags, and descriptions.

### Examples

Polyglot code example viewer with language tabs. Supports copy-to-clipboard and shows code examples grouped by language from `docs.examples[]`.

## Using Components

Import the components in any `.mdx` page:

```mdx
---
import TypeSignature from '../../../components/TypeSignature.astro';
import MemberList from '../../../components/MemberList.astro';
import JsonContract from '../../../components/JsonContract.astro';
import Examples from '../../../components/Examples.astro';
---

<TypeSignature type={typeData} />
<MemberList members={typeData.members} />
<JsonContract json={typeData.json} typeName={typeData.name} />
<Examples examples={typeData.docs?.examples} />
```

## Schema Loader

The `schema-loader.mjs` plugin provides functions to load and query schemas:

- `loadSchemas(basePath)` — Load root schema and all assembly schemas, build type/namespace indexes
- `buildTypeContext(type, rootSchema)` — Build a rendering context for a type
- `buildMemberContext(member)` — Build a rendering context for a member
- `resolvePlaceholders(template, context)` — Replace `{path.to.value}` placeholders
- `getTypeMap(rootSchema, langId)` — Get CLR→target type mappings for a language
- `mapType(clrType, typeMap)` — Map a single CLR type to a target language type
- `generateSearchIndex(entries, outputPath)` — Write a search index JSON file
