import { test as setup, expect } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';

const AUTH_STATE_PATH = 'src/.auth/user.json';

// Cold-start budget: federation init + APP_INITIALIZER + vite on-demand
// compile of the /auth/* lazy chunk + navigation to Keycloak + cold JVM
// handling the first auth request + token exchange + return redirect. On
// the CI runner (also running the full Aspire stack + 4 Angular dev servers),
// Keycloak's first response to /realms/<realm>/protocol/openid-connect/auth
// routinely takes 30-60s. The global 30s test timeout from playwright.config.ts
// isn't enough; bump it to 240s for this single cold-path test.
setup.setTimeout(240_000);

setup('authenticate via Keycloak', async ({ page }) => {
  // Capture token exchange responses
  page.on('response', (r) => {
    if (r.url().includes('/token')) {
      console.log(`Token response: ${r.status()} ${r.url()}`);
    }
  });

  // Capture console errors
  page.on('console', (msg) => {
    if (msg.type() === 'error') console.log(`Console error: ${msg.text()}`);
  });

  // Capture page errors (uncaught exceptions, promise rejections) which
  // the 'console' listener misses.
  page.on('pageerror', (err) => console.log(`PageError: ${err.message}`));
  page.on('requestfailed', (req) =>
    console.log(`RequestFailed: ${req.failure()?.errorText} ${req.url()}`),
  );

  // 1. Go to login. Shell bootstrap chains federation init + APP_INITIALIZER
  //    (Keycloak discovery + language + theme) + vite on-demand compile of the
  //    /auth/* lazy chunk, all on the cold path in CI. `load` fires when bundles
  //    finish downloading, not when Angular has rendered, so we explicitly wait
  //    for content inside <yn-root> before asserting on the button.
  await page.goto('/auth/login', { waitUntil: 'domcontentloaded', timeout: 60_000 });

  await page.locator('yn-root > *').first().waitFor({ timeout: 60_000 });

  // 2. Click sign in
  await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible({ timeout: 30_000 });
  await page.getByRole('button', { name: /sign in/i }).click();

  // 3. Wait for and fill Keycloak form. First hit to the auth endpoint can
  //    take 30-60s on a cold Keycloak JVM in CI; give it 90s headroom.
  await page.waitForURL('**/realms/**', { timeout: 90_000 });
  await page.locator('#username').fill(E2E_USER);
  await page.locator('#password').fill(E2E_PASSWORD);
  await page.locator('#kc-login').click();

  // 4. Wait for redirect — code exchange happens automatically.
  await page.waitForURL('http://localhost:4200/**', { timeout: 30_000 });
  console.log('Redirect URL:', page.url().substring(0, 80) + '...');

  // 5. Wait for Angular to process the code exchange
  //    The APP_INITIALIZER should handle this before routing starts
  await page.waitForTimeout(8000);
  console.log('After wait:', page.url());

  // Check what ended up in storage
  const storageCheck = await page.evaluate(() => ({
    ssToken: !!sessionStorage.getItem('access_token'),
    lsToken: !!localStorage.getItem('access_token'),
    ssKeys: Object.keys(sessionStorage).sort(),
  }));
  console.log('Storage:', JSON.stringify(storageCheck));

  await page.context().storageState({ path: AUTH_STATE_PATH });
});
