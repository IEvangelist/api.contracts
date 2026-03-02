import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import starlightCatppuccin from '@catppuccin/starlight';
import { pluginLineNumbers } from '@expressive-code/plugin-line-numbers';
import { pluginCollapsibleSections } from '@expressive-code/plugin-collapsible-sections';
import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

/* ------------------------------------------------------------------ */
/*  Dynamic sidebar: read package data files and build reference tree  */
/* ------------------------------------------------------------------ */
const __dirname = dirname(fileURLToPath(import.meta.url));
const packagesDir = join(__dirname, 'src', 'data', 'packages');

function slugify(name) {
  return name
    .replace(/([a-z0-9])([A-Z])/g, '$1-$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1-$2')
    .toLowerCase();
}

function loadReferenceSidebar() {
  let files;
  try {
    files = readdirSync(packagesDir).filter(f => f.endsWith('.json'));
  } catch {
    return [];
  }

  const packages = files.map(f =>
    JSON.parse(readFileSync(join(packagesDir, f), 'utf-8')),
  );

  return packages
    .sort((a, b) => a.package.name.localeCompare(b.package.name))
    .map(pkg => ({
      label: pkg.package.name,
      items: [
        { label: 'Overview', link: `/reference/${pkg.package.name}/` },
        ...pkg.types
          .slice()
          .sort((a, b) => a.name.localeCompare(b.name))
          .map(t => ({
            label: t.name,
            link: `/reference/${pkg.package.name}/${slugify(t.name)}/`,
          })),
      ],
    }));
}

export default defineConfig({
  site: 'https://ievangelist.github.io',
  base: '/api.contracts',
  integrations: [
    starlight({
      title: 'API Contracts',
      description: 'Deterministic, signed, versioned JSON schemas for .NET API surfaces.',
      plugins: [
        starlightCatppuccin({
          dark: { flavor: 'macchiato', accent: 'sapphire' },
          light: { flavor: 'latte', accent: 'sapphire' },
        }),
      ],
      expressiveCode: {
        plugins: [pluginLineNumbers(), pluginCollapsibleSections()],
        defaultProps: {
          showLineNumbers: true,
          overridesByLang: {
            'bash,sh,shell': { showLineNumbers: false },
            'xml': { showLineNumbers: true },
          },
        },
      },
      social: [
        {
          icon: 'github',
          label: 'GitHub',
          href: 'https://github.com/IEvangelist/api.contracts',
        },
        {
          icon: 'external',
          label: 'David Pine',
          href: 'https://davidpine.dev',
        },
      ],
      sidebar: [
        {
          label: 'Getting Started',
          items: [
            { label: 'Introduction', link: '/guides/introduction/' },
            { label: 'Quick Start', link: '/guides/quick-start/' },
            { label: 'Configuration', link: '/guides/configuration/' },
          ],
        },
        {
          label: 'Schema Format',
          items: [
            { label: 'Root Schema', link: '/schema/root-schema/' },
            { label: 'Assembly Schema', link: '/schema/assembly-schema/' },
            { label: 'Canonical Hashing', link: '/schema/canonical-hashing/' },
            { label: 'Signatures', link: '/schema/signatures/' },
          ],
        },
        {
          label: 'Sample Schema',
          items: [
            { label: 'SampleApi Schema', link: '/samples/sample-api-schema/' },
          ],
        },
        {
          label: 'API Reference',
          items: [
            { label: 'Overview', link: '/reference/' },
            ...loadReferenceSidebar(),
          ],
        },
      ],
    }),
  ],
});
