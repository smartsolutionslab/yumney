import nx from '@nx/eslint-plugin';

export default [
  ...nx.configs['flat/base'],
  ...nx.configs['flat/typescript'],
  ...nx.configs['flat/javascript'],
  {
    ignores: ['**/dist', '**/vite.config.*.timestamp*', '**/vitest.config.*.timestamp*', '**/federation.config.js'],
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
    rules: {
      '@nx/enforce-module-boundaries': [
        'error',
        {
          enforceBuildableLibDependency: true,
          allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$', '@yumney/shared/.+'],
          depConstraints: [
            {
              sourceTag: 'scope:shared',
              onlyDependOnLibsWithTags: ['scope:shared'],
            },
            {
              sourceTag: 'scope:shell',
              onlyDependOnLibsWithTags: ['scope:shell', 'scope:shared'],
            },
            {
              sourceTag: 'scope:recipes',
              onlyDependOnLibsWithTags: ['scope:recipes', 'scope:shared'],
            },
            {
              sourceTag: 'scope:shopping',
              onlyDependOnLibsWithTags: ['scope:shopping', 'scope:shared'],
            },
            {
              sourceTag: 'scope:account',
              onlyDependOnLibsWithTags: ['scope:account', 'scope:shared'],
            },
            {
              sourceTag: 'scope:shop',
              onlyDependOnLibsWithTags: ['scope:shop', 'scope:shared'],
            },
            {
              sourceTag: 'scope:api',
              onlyDependOnLibsWithTags: ['scope:api', 'scope:shared'],
            },
            {
              sourceTag: 'type:data',
              onlyDependOnLibsWithTags: ['type:data'],
            },
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.ts', '**/*.tsx'],
    ignores: ['**/*.spec.ts', '**/*.spec.tsx', '**/*.stories.ts', '**/*.stories.tsx'],
    rules: {
      'max-lines': ['error', { max: 300, skipBlankLines: true, skipComments: true }],
    },
  },
  {
    files: ['apps/recipes/src/**/*.ts', 'apps/shopping/src/**/*.ts', 'apps/account/src/**/*.ts'],
    ignores: ['apps/*/src/app/api.ts'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: '@yumney/shared/api-client',
              message:
                'Import from the MFE facade (./api or ../api, etc.) instead of @yumney/shared/api-client. Add any missing exports to the facade first.',
            },
          ],
        },
      ],
    },
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.cts', '**/*.mts', '**/*.js', '**/*.jsx', '**/*.cjs', '**/*.mjs'],
    // Override or add rules here
    rules: {},
  },
  {
    // Hard sleeps (page.waitForTimeout / await page.waitForTimeout) are the
    // top flake source in e2e specs. Use polling assertions or
    // locator.waitFor() instead. See issue #401.
    files: ['apps/shell-e2e/src/**/*.ts'],
    rules: {
      'no-restricted-syntax': [
        'error',
        {
          selector: "CallExpression[callee.property.name='waitForTimeout']",
          message:
            'page.waitForTimeout() is forbidden in e2e specs (#401). Use expect.poll, locator.waitFor, or a polling assertion that ties to the deterministic signal you are actually waiting for.',
        },
      ],
    },
  },
];
