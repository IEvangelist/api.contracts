# Skill: API Contracts Schema Consumer

## Purpose

Teach an AI agent how to consume, interpret, and act on emitted API surface data files and the root `schema.json`. The agent can plan API calls, generate code, produce documentation, validate inputs, and reason about .NET API surfaces deterministically.

## Schema Sources

- `ai-skills/apis/schema.json` — Root schema with language definitions, documentation templates, and placeholders.
- `ai-skills/apis/assets/*.json` — Per-assembly API surface data files.

## Interpretation Rules

### Types

- Each entry in `types[]` describes a public type (class, struct, interface, enum, record, delegate).
- Use `name` and `docs.summary` for display and description.
- `kind` indicates the type category; `accessibility` is always `"public"` unless internals are included.

### Members

- Each type has `members[]` containing methods, properties, fields, events, and constructors.
- `signature` provides the full method/property signature for display and matching.
- `parameters[]` includes name, type, nullability, optionality, and default values.

### Documentation

- `docs.summary` provides a one-line description.
- `docs.remarks` provides extended explanation.
- `docs.parameters` maps parameter names to descriptions.
- `docs.examples[]` provides code samples with `language`, `region`, and `code`.

## Context Objects

When rendering templates or reasoning about API surfaces, build these context objects:

```json
{
    "typeContext": {
        "name": "{type.name}",
        "fullName": "{type.fullName}",
        "namespace": "{type.namespace}",
        "kind": "{type.kind}",
        "docs": "{type.docs}",
        "members": "{type.members[]}"
    },
    "memberContext": {
        "name": "{member.name}",
        "kind": "{member.kind}",
        "signature": "{member.signature}",
        "returnType": "{member.returnType}",
        "parameters": "{member.parameters[]}",
        "docs": "{member.docs}"
    },
    "packageContext": {
        "name": "{package.name}",
        "version": "{package.version}",
        "targetFramework": "{package.targetFramework}",
        "apiHash": "{apiHash}"
    }
}
```

## Planning & Execution Model

1. **Load** — Read `schema.json` and all per-assembly data files from `assets/`. Verify signatures if present.
2. **Index** — Build a searchable index of types, members, and examples. Key by `fullName`, `docs.summary`, and `namespace`.
3. **Interpret** — Map user intent to candidate API targets:
    - Search by `docs.summary`, `fullName`, or `namespace`.
    - Rank by relevance using `docs.remarks`.
4. **Plan** — Construct a minimal action sequence:
    - Identify target type and member.
    - Validate required parameters using `parameters[]`.
    - Build request payload from type members and parameter definitions.
5. **Execute** — Invoke the planned action (API call, code generation, or documentation rendering).
6. **Validate** — Check response against return type, nullability, and expected shape.
7. **Reflect** — On failure, consult `docs.examples[]`, fill missing `required` fields, correct type mismatches, and retry.
8. **Produce** — Emit final output. Optionally render documentation using `documentation.templates` and `documentation.placeholders` from the root schema.

## Template & Placeholder Usage

The root schema defines `documentation.templates` and `documentation.placeholders`:

- **Templates** map page types to template files (e.g., `"type": "TypePage.mdx"`).
- **Placeholders** use the syntax `{path.to.value}` and are replaced with context object values.

Example: `{type.fullName}` → `"SampleApi.Customer"`, `{member.signature}` → `"Task<Customer> CreateAsync(Customer customer)"`.

## Polyglot Code Generation Rules

When generating code from schema data:

1. Use `languages.available[]` from the root schema to determine supported target languages.
2. Map CLR types to target language types:
    - `string` → `string` (C#), `string` (TS), `str` (Python), `String` (Java)
    - `int` → `int` (C#), `number` (TS), `int` (Python), `int` (Java)
    - `bool` → `bool` (C#), `boolean` (TS), `bool` (Python), `boolean` (Java)
    - `Guid` → `Guid` (C#), `string` (TS), `str` (Python), `UUID` (Java)
    - `List<T>` → `List<T>` (C#), `T[]` (TS), `list[T]` (Python), `List<T>` (Java)
3. Use member `returnType` and `parameters[]` to determine field types.
4. Mark nullable return types and parameters as optional in the target language.
5. Enforce required properties (from `members[]` with appropriate attributes) in constructors or builders.

## Validation & Safety Rules

- **Required fields**: Every property with `"required": true` must be provided. Fail fast if missing.
- **Nullable fields**: Properties with `"nullable": true` may be `null`/`undefined`/`None`. Do not assume a value.
- **Enum values**: For enum types, only use values from `enumMembers[].name`. Reject unknown values.
- **Type safety**: Match parameter types exactly. Do not coerce `string` to `int` or vice versa.
- **Ignored fields**: Properties with `[JsonIgnore]` attribute should not appear in serialized payloads.
- **Hash verification**: If `apiHash` is present, verify it matches the expected version before acting.
- **Signature verification**: If `signature` envelope is present, verify before trusting schema content.

## Examples

### Example 1: Find and describe an API type

**User intent**: "What is the Customer type?"

**Agent plan**:

1. Search `types[]` where `name == "Customer"` or `fullName` contains `"Customer"`.
2. Return `docs.summary` and list `members[]` with signatures.

**Result**:

> **Customer** — A customer entity with contact information and order history.
>
> Members:
>
> - `Guid Id` (required)
> - `string FullName` (required)
> - `string? Email` (nullable)
> - `DateTimeOffset CreatedAt`
> - `bool IsActive`
> - `List<string> Tags`
> - `ContactMethod PreferredContact`

### Example 2: Generate a TypeScript interface from schema

**User intent**: "Generate a TypeScript interface for the Customer type."

**Agent plan**:

1. Find type `Customer` in schema.
2. Read `members[]` where `kind == "property"`.
3. Map each property's `returnType` to TypeScript type using polyglot rules.

**Result**:

```typescript
export interface Customer {
    id: string; // Guid → string
    fullName: string;
    email?: string; // nullable
    createdAt: string; // DateTimeOffset → string
    isActive: boolean;
    tags: string[];
    preferredContact: ContactMethod;
}
```

### Example 3: Build a request payload

**User intent**: "Create a new customer named Alice."

**Agent plan**:

1. Find `ICustomerService.CreateAsync(Customer customer)`.
2. Identify required fields from `members[]`: `Id`, `FullName`.
3. Build payload using property names.

**Result**:

```json
{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "fullName": "Alice",
    "isActive": true,
    "tags": [],
    "preferredContact": "Email"
}
```

### Example 4: Error recovery — fill missing required fields

**User intent**: "Submit this customer: `{ "fullName": "Bob" }`"

**Agent plan**:

1. Find `Customer` type and read `members[]`.
2. Validate payload against required properties.
3. Detect missing `id` (required, type `Guid`).
4. Generate a default value: `Guid.NewGuid()`.
5. Re-validate: all required fields present.
6. Submit corrected payload.

**Remediation steps**:

- **Missing required field**: Auto-generate a default value if the type is deterministic (`Guid` → new GUID, `DateTime` → `DateTime.UtcNow`). Otherwise, prompt the user.
- **Type mismatch**: Attempt coercion only for safe conversions (`"42"` → `42` for numeric fields). Reject unsafe coercions.
- **Unknown enum value**: List valid values from `enumMembers[].name` and ask the user to choose.
- **Null in non-nullable field**: Replace with a sensible default or ask the user.
- **Retry logic**: After each correction, re-validate the full payload before proceeding. Maximum 3 retry attempts.

**Corrected result**:

```json
{
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "fullName": "Bob",
    "isActive": true,
    "tags": [],
    "preferredContact": "Email"
}
```

## Telemetry & Auditing

When acting on schema data, log the following for traceability:

- `schemaVersion` — The version of the schema format.
- `package.name` — The assembly name.
- `package.version` — The assembly version.
- `apiHash` — The deterministic hash of the API surface.
- `signature.verified` — Whether the signature was verified (true/false/not-present).
- `action` — The action taken (e.g., "describe-type", "generate-dto", "build-request").
- `targetType` — The `fullName` of the target type.
- `targetMember` — The member name if applicable.
