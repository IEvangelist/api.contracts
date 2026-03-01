/**
 * Shared schema data module.
 * Loads the SampleApi schema once and provides type lookup by name.
 */

import { loadSchemas } from '../plugins/schema-loader.mjs';

const { assemblySchemas, typeIndex } = loadSchemas();

/**
 * Get type data by simple name from the loaded schemas.
 * @param {string} name - The simple type name (e.g., "Address")
 * @returns {Object|undefined} The type data, or undefined if not found
 */
export function getTypeByName(name) {
  for (const schema of assemblySchemas) {
    const type = (schema.types || []).find(t => t.name === name);
    if (type) return { ...type, package: schema.package };
  }
  return undefined;
}

export { assemblySchemas, typeIndex };
