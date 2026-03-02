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

export const kindIcons: Record<string, string> = {
  class: 'C',
  record: 'R',
  'record struct': 'RS',
  struct: 'S',
  interface: 'I',
  enum: 'E',
  delegate: 'D',
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

export const memberKindIcons: Record<string, string> = {
  constructor: 'C',
  property: 'P',
  method: 'M',
  field: 'F',
  event: 'E',
  indexer: 'I',
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
  const simpleName = fullName.split('.').pop() ?? fullName;
  return { prefix, fullName, simpleName };
}
