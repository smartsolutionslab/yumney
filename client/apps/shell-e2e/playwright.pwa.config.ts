import { defineConfig } from '@playwright/test';

/**
 * PWA E2E tests run against a static production build.
 *
 * Prerequisites:
 *   nx build shell --configuration=production
 *
 * The tests use `serve` to host the built app and validate
 * manifest, service worker registration, and offline behavior.
 */
export default defineConfig({
  testDir: './src/pwa',
  fullyParallel: false,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 2 : 0,
  workers: 1,
  timeout: 60000,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4280',
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'npx serve dist/apps/shell/browser -l 4280 -s',
    port: 4280,
    reuseExistingServer: !process.env['CI'],
    cwd: '../..',
  },
});
