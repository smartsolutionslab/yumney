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
  // Serial runs. File-level parallelism was tried (#331) and broke the
  // recipes + shopping suites — those specs share DB state across tests in
  // a file via test.beforeAll, and two files running concurrently corrupted
  // each other's fixtures. Revisit once those specs are refactored to use
  // per-test fixtures.
  workers: 1,
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
