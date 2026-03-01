import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import starlightCatppuccin from '@catppuccin/starlight';
import { pluginLineNumbers } from '@expressive-code/plugin-line-numbers';
import { pluginCollapsibleSections } from '@expressive-code/plugin-collapsible-sections';

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
          autogenerate: { directory: 'reference' },
        },
      ],
    }),
  ],
});
