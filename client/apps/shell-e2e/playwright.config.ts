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
  // Refuse to start if BASE_URL/GATEWAY_URL points outside localhost. The
  // suite issues real writes; set E2E_ALLOW_REMOTE=true to override.
  globalSetup: require.resolve('./src/global-setup'),
  // Bumped from 30s to 60s: post-token-expiry-fix, fixture-heavy beforeAll
  // hooks (openAuthenticatedPage + API POST under parallel worker pressure)
  // started exceeding 30s. The real API calls themselves are ~1s; the budget
  // is eaten by the `page.goto('/')` that scopes localStorage to the origin
  // plus concurrent Angular bootstrap on the shared dev server.
  timeout: 90_000,
  // 45s mirrors TIMEOUTS.default — covers MFE federation cold-start under
  // parallel worker pressure (see helpers/timeouts.ts).
  expect: { timeout: 45_000 },
  // 1 retry in CI: backend writes through Wolverine messaging + projections
  // create eventual-consistency races that pass on retry. The 4 min cost
  // is worth the stable signal — without it, transient flakes look like
  // real failures and noise dominates the CI summary.
  retries: process.env['CI'] ? 1 : 0,
  // File-level parallelism on CI. fullyParallel stays false so tests inside
  // a spec file still run sequentially on their assigned worker — the
  // recipes/shopping specs use beforeAll to create shared DB fixtures and
  // rely on that order. Inter-file safety: each beforeAll uses
  // uniqueTitle() with a random suffix so two files' setups don't clash
  // when they run concurrently on different workers, and each beforeAll
  // reuses auth.setup's storageState via openAuthenticatedPage() rather
  // than replaying the brittle Keycloak UI login flow.
  // Within-shard parallelism. The CI workflow also splits across N matrix
  // shards (see .github/workflows/e2e.yml) so total concurrency is workers
  // × shards. Each ubuntu-latest runner has 4 vCPU.
  workers: process.env['CI'] ? 4 : undefined,
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
    // Tell the browser to prefer reduced motion; our CSS suppresses keyframe
    // animations under @media (prefers-reduced-motion: reduce), so things
    // like the confirm-dialog overlay render at static opacity:1 instead of
    // racing through a fade-in that Playwright sometimes catches at opacity:0.
    reducedMotion: 'reduce',
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
        // Force chromium to HTTP/1.1. Aspire's DCP localhost proxy doesn't
        // round-trip HTTP/2 streams in our setup — browser-issued cross-origin
        // fetches to the gateway silently failed when chromium negotiated h2,
        // while curl/Playwright server-side fetches (HTTP/1.1 by default)
        // worked. Confirmed by gateway request log: with --disable-http2 the
        // GETs land and tests pass; without it, they never arrive. Keep this
        // until DCP h2 support stabilizes upstream.
        launchOptions: {
          args: ['--disable-http2'],
        },
      },
      dependencies: ['setup'],
    },
  ],
});
