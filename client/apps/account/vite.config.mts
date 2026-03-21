/// <reference types="vitest" />
import { defineConfig } from 'vite';
import angular from '@analogjs/vite-plugin-angular';

export default defineConfig(({ mode }) => ({
  plugins: [angular()],
  test: {
    name: 'account',
    globals: true,
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
    reporters: ['default'],
    cacheDir: '../../node_modules/.vite/apps/account',
  },
  define: {
    'import.meta.vitest': mode !== 'production',
  },
}));
