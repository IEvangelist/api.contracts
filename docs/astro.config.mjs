import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

export default defineConfig({
  site: 'https://ievangelist.github.io',
  base: '/api.contracts',
  integrations: [
    starlight({
      title: 'API Contracts',
      description: 'Deterministic, signed, versioned JSON schemas for .NET API surfaces.',
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
          label: 'API Reference',
          autogenerate: { directory: 'reference' },
        },
      ],
    }),
  ],
});
