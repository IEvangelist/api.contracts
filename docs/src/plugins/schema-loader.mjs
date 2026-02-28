/**
 * Schema Loader Plugin for Astro/Starlight
 *
 * Loads ai-skills/apis/schema.json and ai-skills/apis/reference/*.ai-schema.json
 * and exposes them as context objects for templates and components.
 */

import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { join, resolve } from 'node:path';

/**
 * @typedef {Object} SchemaContext
 * @property {Object} rootSchema - The root schema.json data
 * @property {Object[]} assemblySchemas - Array of assembly schema objects
 * @property {Map<string, Object>} typeIndex - Index of all types by fullName
 * @property {Map<string, Object[]>} namespaceIndex - Index of types by namespace
 */

/**
 * Load all schemas from the ai-skills directory.
 * @param {string} [basePath] - Path to the repository root
 * @returns {SchemaContext}
 */
export function loadSchemas(basePath = resolve('..')) {
  const schemaPath = join(basePath, 'ai-skills', 'apis', 'schema.json');
  const referencePath = join(basePath, 'ai-skills', 'apis', 'reference');

  // Load root schema
  const rootSchema = JSON.parse(readFileSync(schemaPath, 'utf-8'));

  // Load assembly schemas
  const assemblySchemas = [];
  if (existsSync(referencePath)) {
    const files = readdirSync(referencePath).filter(f => f.endsWith('.ai-schema.json'));
    for (const file of files) {
      const data = JSON.parse(readFileSync(join(referencePath, file), 'utf-8'));
      assemblySchemas.push({ fileName: file, ...data });
    }
  }

  // Build indexes
  const typeIndex = new Map();
  const namespaceIndex = new Map();

  for (const schema of assemblySchemas) {
    for (const type of (schema.types || [])) {
      typeIndex.set(type.fullName, { ...type, package: schema.package });

      if (!namespaceIndex.has(type.namespace)) {
        namespaceIndex.set(type.namespace, []);
      }
      namespaceIndex.get(type.namespace).push(type);
    }
  }

  return { rootSchema, assemblySchemas, typeIndex, namespaceIndex };
}

/**
 * Build a context object for template rendering.
 * @param {Object} type - A type from the schema
 * @param {Object} rootSchema - The root schema
 * @returns {Object} Context object with resolved placeholders
 */
export function buildTypeContext(type, rootSchema) {
  return {
    name: type.ai?.name || type.name,
    fullName: type.fullName,
    namespace: type.namespace,
    kind: type.kind,
    summary: type.docs?.summary || '',
    remarks: type.docs?.remarks || '',
    category: type.ai?.category || '',
    role: type.ai?.role || '',
    tags: type.ai?.tags || [],
    members: (type.members || []).map(m => buildMemberContext(m)),
    json: type.json || null,
    enumMembers: type.enumMembers || null,
  };
}

/**
 * Build a context object for a member.
 * @param {Object} member - A member from the schema
 * @returns {Object}
 */
export function buildMemberContext(member) {
  return {
    name: member.ai?.name || member.name,
    kind: member.kind,
    signature: member.signature,
    returnType: member.returnType || null,
    isReturnNullable: member.isReturnNullable || false,
    parameters: member.parameters || [],
    summary: member.docs?.summary || '',
    returns: member.docs?.returns || '',
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

function getNestedValue(obj, path) {
  return path.split('.').reduce((current, key) => {
    return current && typeof current === 'object' ? current[key] : undefined;
  }, obj);
}
