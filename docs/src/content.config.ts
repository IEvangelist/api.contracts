import { docsLoader } from '@astrojs/starlight/loaders';
import { docsSchema } from '@astrojs/starlight/schema';
import { defineCollection, z } from 'astro:content';
import { glob } from 'astro/loaders';

export const collections = {
  docs: defineCollection({ loader: docsLoader(), schema: docsSchema() }),

  /**
   * Package API schemas — drop `{Package}.{version}.json` files into
   * `src/data/packages/` and every reference page is generated automatically.
   */
  packages: defineCollection({
    loader: glob({ pattern: '**/*.json', base: './src/data/packages' }),
    schema: z
      .object({
        $schema: z.string().optional(),
        schemaVersion: z.string(),
        package: z.object({
          name: z.string(),
          version: z.string(),
          targetFramework: z.string(),
        }),
        apiHash: z.string(),
        types: z.array(z.any()),
      })
      .passthrough(),
  }),
};
