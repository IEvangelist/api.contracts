/* ------------------------------------------------------------------ */
/*  Shared helpers for the auto-generated API-reference pages.        */
/* ------------------------------------------------------------------ */

/** Convert a PascalCase type name to a kebab-case URL slug. */
export function slugify(name: string): string {
  return name
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .toLowerCase();
}

/* ---- kind helpers ------------------------------------------------ */

export const kindOrder = [
  'class',
  'record',
  'record struct',
  'struct',
  'interface',
  'enum',
  'delegate',
] as const;

export const kindLabels: Record<string, string> = {
  class: 'Classes',
  record: 'Records',
  'record struct': 'Record Structs',
  struct: 'Structs',
  interface: 'Interfaces',
  enum: 'Enums',
  delegate: 'Delegates',
};

export const memberKindOrder = [
  'constructor',
  'property',
  'method',
  'field',
  'event',
  'indexer',
] as const;

export const memberKindLabels: Record<string, string> = {
  constructor: 'Constructors',
  property: 'Properties',
  method: 'Methods',
  field: 'Fields',
  event: 'Events',
  indexer: 'Indexers',
};

/* ---- type helpers ------------------------------------------------ */

/** Group an array of types by their `kind`, maintaining a meaningful order. */
export function groupTypesByKind(types: any[]): Map<string, any[]> {
  const groups = new Map<string, any[]>();
  for (const kind of kindOrder) {
    const matching = types.filter((t: any) => t.kind === kind);
    if (matching.length > 0) {
      groups.set(
        kind,
        matching.sort((a: any, b: any) => a.name.localeCompare(b.name)),
      );
    }
  }
  return groups;
}

/** Group members by their `kind`, maintaining a meaningful order. */
export function groupMembersByKind(members: any[]): Map<string, any[]> {
  const groups = new Map<string, any[]>();
  for (const kind of memberKindOrder) {
    const matching = members.filter((m: any) => m.kind === kind);
    if (matching.length > 0) {
      groups.set(kind, matching);
    }
  }
  return groups;
}

/* ---- link helpers ------------------------------------------------ */

/**
 * Build an absolute href for a type page.
 * `base` is `import.meta.env.BASE_URL` (e.g. `/api.contracts`).
 */
export function typeHref(
  base: string,
  packageName: string,
  typeName: string,
): string {
  const b = base.replace(/\/$/, '');
  return `${b}/reference/${packageName}/${slugify(typeName)}/`;
}

/** NuGet package page URL. */
export function nugetHref(packageName: string): string {
  return `https://www.nuget.org/packages/${packageName}`;
}

/**
 * Try to resolve a CLR type string (e.g. `SampleApi.Customer`, `System.Guid`)
 * to a link within the current reference. Returns `null` when the type is
 * external or cannot be resolved.
 */
export function resolveTypeLink(
  raw: string,
  types: any[],
  base: string,
  packageName: string,
): { href: string; label: string } | null {
  // Strip nullable markers and collection wrappers for matching.
  const clean = raw
    .replace(/\?$/, '')
    .replace(/^System\.Threading\.Tasks\.Task<(.+)>$/, '$1')
    .replace(/^System\.Collections\.Generic\.\w+<(.+)>$/, '$1');

  const match = types.find(
    (t: any) => t.fullName === clean || t.fullName === raw,
  );
  if (match) {
    return {
      href: typeHref(base, packageName, match.name),
      label: match.name,
    };
  }
  return null;
}

/**
 * Shorten a fully-qualified generic type name to its simple form.
 *
 * Handles dots inside generic arguments correctly:
 *   `System.IEquatable<SampleApi.ValidationError>`  →  `IEquatable<ValidationError>`
 *   `SampleApi.PagedResult<T>`                      →  `PagedResult<T>`
 *   `System.Collections.Generic.List<System.String>` →  `List<String>`
 */
export function shortTypeName(fullName: string): string {
  // Find the first top-level '<'
  let firstAngle = -1;
  for (let i = 0; i < fullName.length; i++) {
    if (fullName[i] === '<') { firstAngle = i; break; }
  }

  if (firstAngle < 0) {
    // No generics — simple dot-split is safe
    return fullName.split('.').pop() ?? fullName;
  }

  // Outer type name is everything before the '<'
  const outerShort = fullName.slice(0, firstAngle).split('.').pop() ?? fullName;

  // Generic args content between first '<' and last '>'
  const lastAngle = fullName.lastIndexOf('>');
  const argsContent = fullName.slice(firstAngle + 1, lastAngle);

  // Split on top-level commas (not inside nested <>)
  const args: string[] = [];
  let current = '';
  let depth = 0;
  for (const ch of argsContent) {
    if (ch === '<') depth++;
    if (ch === '>') depth--;
    if (ch === ',' && depth === 0) {
      args.push(current.trim());
      current = '';
    } else {
      current += ch;
    }
  }
  if (current.trim()) args.push(current.trim());

  // Recursively shorten each arg
  const shortArgs = args.map(a => shortTypeName(a));
  return `${outerShort}<${shortArgs.join(', ')}>`;
}

/**
 * Parse a `seeAlso` doc-id reference like `T:SampleApi.Customer` or
 * `P:SampleApi.Customer.Id`.
 */
export function parseSeeAlso(ref: string): {
  prefix: string;
  fullName: string;
  simpleName: string;
} {
  const colon = ref.indexOf(':');
  const prefix = colon > 0 ? ref.slice(0, colon) : '';
  const fullName = colon > 0 ? ref.slice(colon + 1) : ref;
  const simpleName = shortTypeName(fullName);
  return { prefix, fullName, simpleName };
}

/* ---- signature formatting ---------------------------------------- */

/**
 * Format a C# member signature with parameters on separate lines.
 * For signatures with multiple parameters, each parameter is placed
 * on its own line with 4-space indentation.
 *
 * Example:
 *   `Task<bool> Foo.Bar(int a, string b)` becomes:
 *   ```
 *   Task<bool> Foo.Bar(
 *       int a,
 *       string b)
 *   ```
 */
export function formatSignature(sig: string): string {
  if (!sig) return sig;

  // Find the outermost parameter list — the last '(' that starts the param block.
  // We need to handle nested generics like Task<IReadOnlyList<T>>.
  const openIdx = findParamListOpen(sig);
  if (openIdx < 0) return sig; // No param list (property, field, event)

  const closeIdx = sig.lastIndexOf(')');
  if (closeIdx <= openIdx) return sig;

  const prefix = sig.slice(0, openIdx + 1); // everything up to and including '('
  const suffix = sig.slice(closeIdx);       // ')' and anything after
  const paramStr = sig.slice(openIdx + 1, closeIdx);

  // Split parameters respecting nested angle brackets and parens.
  const params = splitParams(paramStr);
  if (params.length === 0) return sig; // No params — keep on one line

  const indent = '    ';
  return prefix + '\n' + params.map((p, i) => {
    const sep = i < params.length - 1 ? ',' : '';
    return indent + p.trim() + sep;
  }).join('\n') + suffix;
}

/**
 * Find the index of the opening paren that starts the parameter list.
 * Skips angle-bracket generic sections in the method name portion.
 */
function findParamListOpen(sig: string): number {
  let depth = 0;
  for (let i = 0; i < sig.length; i++) {
    const ch = sig[i];
    if (ch === '<') depth++;
    else if (ch === '>') depth--;
    else if (ch === '(' && depth === 0) return i;
  }
  return -1;
}

/**
 * Split a parameter string by commas, respecting nested generics.
 */
function splitParams(paramStr: string): string[] {
  const parts: string[] = [];
  let depth = 0;
  let current = '';
  for (const ch of paramStr) {
    if (ch === '<' || ch === '(') depth++;
    else if (ch === '>' || ch === ')') depth--;

    if (ch === ',' && depth === 0) {
      parts.push(current);
      current = '';
    } else {
      current += ch;
    }
  }
  if (current.trim()) parts.push(current);
  return parts;
}

/* ---- code formatting --------------------------------------------- */

/**
 * Remove common leading whitespace from a multi-line code string.
 * Finds the minimum indentation across all non-empty lines and strips it.
 */
export function dedent(code: string): string {
  if (!code) return code;
  const lines = code.split('\n');
  // Find minimum indent across non-empty lines that have indentation.
  // Skip lines with zero indent (e.g. first line pasted flush against <code> tag).
  let minIndent = Infinity;
  for (const line of lines) {
    if (line.trim().length === 0) continue;
    const leading = line.match(/^(\s+)/);
    if (leading && leading[1].length < minIndent) {
      minIndent = leading[1].length;
    }
  }
  if (minIndent === 0 || minIndent === Infinity) return code;
  return lines
    .map(line => {
      if (line.trim().length === 0) return '';
      // Only strip from lines that actually have the leading whitespace
      return line.startsWith(' '.repeat(minIndent)) ? line.slice(minIndent) : line;
    })
    .join('\n');
}
