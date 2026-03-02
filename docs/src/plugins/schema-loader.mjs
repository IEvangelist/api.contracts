/**
 * Schema Loader Plugin for Astro/Starlight
 *
 * Loads ai-skills/apis/schema.json and ai-skills/apis/assets/*.api-schema.json
 * and exposes them as context objects for templates and components.
 */

import { readFileSync, readdirSync, existsSync, writeFileSync, mkdirSync } from 'node:fs';
import { join, resolve } from 'node:path';

/**
 * @typedef {Object} SchemaContext
 * @property {Object} rootSchema - The root schema.json data
 * @property {Object[]} assemblySchemas - Array of assembly schema objects
 * @property {Map<string, Object>} typeIndex - Index of all types by fullName
 * @property {Map<string, Object[]>} namespaceIndex - Index of types by namespace
 * @property {Object[]} searchEntries - Flat list of searchable entries
 */

/**
 * Load all schemas from the ai-skills directory.
 * @param {string} [basePath] - Path to the repository root
 * @returns {SchemaContext}
 */
export function loadSchemas(basePath = resolve('..')) {
  const schemaPath = join(basePath, 'ai-skills', 'apis', 'schema.json');
  const referencePath = join(basePath, 'ai-skills', 'apis', 'assets');

  // Load root schema
  const rootSchema = JSON.parse(readFileSync(schemaPath, 'utf-8'));

  // Load assembly schemas
  const assemblySchemas = [];
  if (existsSync(referencePath)) {
    const files = readdirSync(referencePath).filter(f => f.endsWith('.api-schema.json'));
    for (const file of files) {
      const data = JSON.parse(readFileSync(join(referencePath, file), 'utf-8'));
      assemblySchemas.push({ fileName: file, ...data });
    }
  }

  // Build indexes
  const typeIndex = new Map();
  const namespaceIndex = new Map();
  const searchEntries = [];

  for (const schema of assemblySchemas) {
    for (const type of (schema.types || [])) {
      const typeWithPkg = { ...type, package: schema.package };
      typeIndex.set(type.fullName, typeWithPkg);

      if (!namespaceIndex.has(type.namespace)) {
        namespaceIndex.set(type.namespace, []);
      }
      namespaceIndex.get(type.namespace).push(typeWithPkg);

      // Add to search entries
      searchEntries.push({
        kind: 'type',
        name: type.name,
        fullName: type.fullName,
        namespace: type.namespace,
        typeKind: type.kind,
        summary: type.docs?.summary ?? '',
        package: schema.package?.name ?? '',
      });

      for (const member of (type.members || [])) {
        searchEntries.push({
          kind: 'member',
          name: member.name,
          fullName: `${type.fullName}.${member.name}`,
          namespace: type.namespace,
          memberKind: member.kind,
          signature: member.signature,
          summary: member.docs?.summary ?? '',
          parentType: type.fullName,
          package: schema.package?.name ?? '',
        });
      }
    }
  }

  return { rootSchema, assemblySchemas, typeIndex, namespaceIndex, searchEntries };
}

/**
 * Build a context object for template rendering.
 * @param {Object} type - A type from the schema
 * @param {Object} rootSchema - The root schema
 * @returns {Object} Context object with resolved placeholders
 */
export function buildTypeContext(type, rootSchema) {
  return {
    name: type.name,
    fullName: type.fullName,
    namespace: type.namespace,
    kind: type.kind,
    accessibility: type.accessibility,
    summary: type.docs?.summary || '',
    remarks: type.docs?.remarks || '',
    members: (type.members || []).map(m => buildMemberContext(m)),
    enumMembers: type.enumMembers || null,
    baseType: type.baseType || null,
    interfaces: type.interfaces || [],
    attributes: type.attributes || [],
    isAbstract: type.isAbstract || false,
    isSealed: type.isSealed || false,
    isStatic: type.isStatic || false,
    isGeneric: type.isGeneric || false,
    genericParameters: type.genericParameters || [],
    package: type.package || null,
  };
}

/**
 * Build a context object for a member.
 * @param {Object} member - A member from the schema
 * @returns {Object}
 */
export function buildMemberContext(member) {
  return {
    name: member.name,
    kind: member.kind,
    accessibility: member.accessibility,
    signature: member.signature,
    returnType: member.returnType || null,
    isReturnNullable: member.isReturnNullable || false,
    isStatic: member.isStatic || false,
    isAbstract: member.isAbstract || false,
    isVirtual: member.isVirtual || false,
    isOverride: member.isOverride || false,
    isAsync: member.isAsync || false,
    parameters: member.parameters || [],
    summary: member.docs?.summary || '',
    remarks: member.docs?.remarks || '',
    returns: member.docs?.returns || '',
    examples: member.docs?.examples || [],
    attributes: member.attributes || [],
  };
}

/**
 * Replace placeholders in a template string using a context object.
 * @param {string} template - Template string with {path.to.value} placeholders
 * @param {Object} context - Context object
 * @returns {string}
 */
export function resolvePlaceholders(template, context) {
  return template.replace(/\{([^}]+)\}/g, (match, path) => {
    const value = getNestedValue(context, path);
    return value !== undefined ? String(value) : match;
  });
}

/**
 * Generate a search index JSON file from loaded schemas.
 * @param {Object[]} searchEntries - Search entries from loadSchemas
 * @param {string} outputPath - Path to write the search index
 */
export function generateSearchIndex(searchEntries, outputPath) {
  const dir = resolve(outputPath, '..');
  if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  writeFileSync(outputPath, JSON.stringify(searchEntries, null, 2), 'utf-8');
}

/**
 * Get a language's type mapping from the root schema.
 * @param {Object} rootSchema - The root schema
 * @param {string} langId - Language id (e.g., 'typescript')
 * @returns {Object|null} Type map or null
 */
export function getTypeMap(rootSchema, langId) {
  const lang = rootSchema.languages?.available?.find(l => l.id === langId);
  return lang?.typeMap ?? null;
}

/**
 * Map a CLR type to a target language type.
 * @param {string} clrType - The CLR type name
 * @param {Object} typeMap - Type mapping from the root schema
 * @returns {string} The mapped type or the original type
 */
export function mapType(clrType, typeMap) {
  if (!typeMap) return clrType;
  return typeMap[clrType] ?? clrType;
}

function getNestedValue(obj, path) {
  return path.split('.').reduce((current, key) => {
    return current && typeof current === 'object' ? current[key] : undefined;
  }, obj);
}
