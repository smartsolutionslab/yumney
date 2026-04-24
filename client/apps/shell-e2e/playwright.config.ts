import { defineConfig, devices } from '@playwright/test';
import { nxE2EPreset } from '@nx/playwright/preset';

/**
 * E2E tests run against the full system.
 *
 * Prerequisites:
 *   dotnet run --project src/Yumney.AppHost
 *
 * This starts Keycloak, PostgreSQL, Redis, all APIs, the Gateway (port 5100),
 * and the Angular frontend (port 4200). Tests hit the real stack — no mocks.
 *
 * Override BASE_URL if running against a different environment:
 *   BASE_URL=https://staging.yumney.com nx e2e shell-e2e
 */
const baseURL = process.env['BASE_URL'] || 'http://localhost:4200';

export default defineConfig({
  ...nxE2EPreset(__filename, { testDir: './src' }),
  timeout: 30_000,
  expect: { timeout: 10_000 },
  // On CI: retry once (not twice) — each retry on a 30s-timeout test can cost
  // a minute on top of the original. Transient flakes still get a second
  // chance; systemic failures fail faster.
  retries: process.env['CI'] ? 1 : 0,
  // File-level parallelism on CI. fullyParallel stays false so tests inside
  // a spec file still run sequentially on their assigned worker — the
  // recipes/shopping specs use beforeAll to create shared DB fixtures and
  // rely on that order. Inter-file safety: each beforeAll uses
  // uniqueTitle() with a random suffix so two files' setups don't clash
  // when they run concurrently on different workers, and each beforeAll
  // reuses auth.setup's storageState via openAuthenticatedPage() rather
  // than replaying the brittle Keycloak UI login flow.
  workers: process.env['CI'] ? 2 : undefined,
  reporter: process.env['CI']
    ? [
        ['html', { open: 'never' }],
        ['junit', { outputFile: '../../test-results/shell-e2e/results.xml' }],
      ]
    : [['html', { open: 'on-failure' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /auth\.setup\.ts/,
    },
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        storageState: 'src/.auth/user.json',
      },
      dependencies: ['setup'],
    },
  ],
});
