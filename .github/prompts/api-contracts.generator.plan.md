### API Contracts Generator (AI Schema Generator)

**Purpose**  
Produce deterministic, signed, versioned JSON schemas that describe a .NET assembly’s public API surface for AI, tooling, and docs.

---

**Primary outputs**

- **Root schema**: `ai-skills/apis/schema.json` (language definitions, templates, placeholders, languages list).  
- **Assembly schemas**: `ai-skills/apis/reference/<AssemblyName>.json` (data-only snapshots referencing the root schema).  
- **Optional vendor mirrors**: `<vendor>/apis/reference/<AssemblyName>.json`.

---

**Core responsibilities**

- Walk public symbols (optionally internals when configured).  
- Normalize XML docs (summary, remarks, params, returns, examples, inline code, inheritdoc).  
- Extract and normalize code samples with `language` and `region`.  
- Model System.Text.Json behavior (jsonName, ignored, nullable, required, contractType).  
- Apply `ApiContractAttribute` overrides (ignore/exclude types from schema).  
- Build a canonical API model and compute a deterministic `apiHash`.  
- Optionally sign the emitted schema with a private key and embed signature metadata.  
- Emit root schema once and one assembly schema per assembly.

---

**Canonical hashing rules**

- **Include**: public types, public members, signatures (param names/types/nullability), attributes that affect shape, JSON serialization metadata, XML docs *excluding* code sample content.  
- **Exclude**: file paths, timestamps, build metadata, code sample content, ordering differences, whitespace.  
- **Process**: build canonical model → sort (namespace → type → member) → serialize to UTF‑8 JSON with sorted properties and no whitespace → SHA‑256 → `apiHash: "sha256:<hex>"`.

---

**Signature envelope**

```json
"signature": {
  "algorithm": "RSA-SHA256",
  "publicKeyId": "pine-2026",
  "value": "base64-signature"
}
```

---

**Configuration surface**

- MSBuild props: `AISchemaEmitStandard`, `AISchemaEmitVendor`, `AISchemaVendorFolder`, `AISchemaSign`, `AISchemaSigningPrivateKey`, `AISchemaIncludeInternals`.  
- Assembly attribute: `ApiContractConfig(OutputFolder = "...", EmitStandard = true, Sign = true, SigningKeyId = "...")`.

---

**Implementation notes**

- Use an **incremental Roslyn source generator** for performance.  
- Cache XML docs per assembly.  
- Produce compact, deterministic JSON.  
- Emit both plaintext and signed variants if configured.  
- Provide a small verification helper in the SDK to validate signatures and compute canonical hashes.

---

**Deliverables & next steps**

- Implement canonical model spec and serializer.  
- Implement hash + signing pipeline.  
- Emit sample `schema.json` and one assembly schema.  
- Provide verification SDK and MSBuild integration.

---

---

### API Docs Generator (Static Site Generator Integration)

**Purpose**  
Consume the emitted schemas to generate versioned, polyglot, template-driven API documentation (Starlight/Astro or any SSG) with full control over templating, placeholders, and context objects.

---

**Primary inputs**

- `ai-skills/apis/schema.json` (root language + templates + placeholders).  
- `ai-skills/apis/reference/*.json` (assembly snapshots).

---

**Core responsibilities**

- Load root schema and all assembly schemas at build time.  
- Build an in-memory documentation model (namespaces, types, members, examples, JSON contracts).  
- Generate pages: namespace index, type pages, member pages, JSON contract pages, examples pages.  
- Render polyglot code samples with tabs, syntax highlighting, and copy buttons.  
- Replace placeholders using context objects derived from schema data.  
- Produce versioned output (folder-based or schema-based).  
- Auto-generate navigation, cross-references, breadcrumbs, and search index.

---

**Template & placeholder model**

- Root schema provides `documentation.templates` mapping (e.g., `type: TypePage.mdx`) and `documentation.placeholders` (e.g., `typeName: "{type.name}"`).  
- SSG replaces placeholders with context objects such as `{ type, member, namespace, package, assembly, apiHash }`.  
- Templates are MDX/MD with components (TypeSignature, MemberList, JsonContract, Examples).

---

**Polyglot code sample handling**

- Render each `docs.examples[]` entry as a tab labeled by `language`.  
- Allow SSG-level language transforms or generated stubs (optional).  
- Provide copy and run affordances where safe.

---

**Versioning strategies**

- **Folder-based**: `docs/v3.4.1/`, `docs/latest/`.  
- **Schema-based**: use `package.version` from assembly schema to drive routing and navigation.  
- SSG can build multiple versions in one run by loading multiple assembly schema snapshots.

---

**Cross-referencing rules**

- Auto-link by `fullName`, `namespace`, and signature types.  
- Resolve `seeAlso` and `cref` to target pages.  
- If multiple versions exist, link to the version matching the current doc context.

---

**SSG pipeline**

1. Load `schema.json`.  
2. Load all per-assembly data files from `reference/`.  
3. Validate signatures and `apiHash` if verification is required.  
4. Build doc model and navigation.  
5. Render templates with placeholders replaced by context objects.  
6. Emit static site and search index.

---

**Deliverables & next steps**

- Provide a Starlight starter site with components and example templates.  
- Provide a plugin/loader that imports `ai-skills` JSON files and exposes context to templates.  
- Provide sample versioned site output and CI integration.

---

---

### Thor Domain — SKILL.md (Agentic AI Skill Authoring)

**Purpose**  
Define a SKILL.md format and operational rules that teach an AI agent how to *use* the emitted schemas agentically: plan, validate, call APIs, generate code, and produce documentation or actions safely and deterministically.

---

**SKILL.md role**

- Declarative behavioral contract for agents.  
- Maps schema artifacts to actionable capabilities.  
- Encodes interpretation rules, validation rules, template usage, and execution patterns.

---

**SKILL.md canonical sections**

- **Skill Overview** — capability summary and intended use cases.  
- **Schema Sources** — paths to `schema.json` and assembly schemas.  
- **Interpretation Rules** — how to read `types`, `members`, `json`, `docs`, `ai` metadata, and examples.  
- **Context Object Model** — canonical context shapes agents should build (e.g., `{type, member, jsonContract, examples, package, assembly}` ).  
- **Planning & Execution Model** — stepwise agent behavior: identify API → validate inputs → construct request → execute or simulate → validate response → format output.  
- **Template & Placeholder Usage** — how to apply `documentation.templates` and `placeholders` with context objects.  
- **Polyglot Code Generation Rules** — how to generate code from signatures and JSON contracts across languages.  
- **Validation & Safety Rules** — required checks (nullability, required fields, enum values), error handling, and retry heuristics.  
- **Examples** — concrete, annotated examples of planning, request construction, codegen, and error recovery.  
- **Telemetry & Auditing** — what to log for traceability (schema version, apiHash, signature verification result, actions taken).

---

**Agentic behavior model (concise)**

1. **Load** root schema and assembly schemas; verify signature if required.  
2. **Index** types, members, JSON contracts, and examples into a searchable model.  
3. **Interpret** user intent and map to candidate API targets using docs and type metadata.  
4. **Plan** a minimal sequence of steps (validate → build → call/simulate → verify → respond).  
5. **Execute** using JSON contract rules; when calling external services, follow safety and rate limits.  
6. **Reflect** on failures and apply deterministic remediation (fill missing required fields, correct types, consult examples).  
7. **Produce** final output and optionally render templates using placeholders.

---

**SKILL.md example skeleton**

````md
# Skill: <Skill Name>

## Purpose
Short description.

## Schema Sources
- ai-skills/apis/schema.json
- ai-skills/apis/reference/*.json

## Interpretation Rules
- Types: use `types[]` and `docs` metadata.
- Members: use `members[]` and `signature`.
- JSON: use `json.properties[]`, `nullable`, `required`.

## Context Objects
- typeContext: { fullName, namespace, docs, json, members }
- memberContext: { name, signature, docs }

## Plan Template
1. Identify candidate API.
2. Validate required fields.
3. Build request per JSON contract.
4. Execute or simulate.
5. Validate response.
6. Render template with placeholders.

## Validation Rules
- Enforce `required` and `nullable`.
- Validate enum values.
- Use examples when ambiguous.

## Examples
- Example 1: map user intent to CreateCustomer method and build request.
- Example 2: generate TypeScript DTO from JSON contract.

## Telemetry
- Emit schemaVersion, package.version, apiHash, signature verification result.
