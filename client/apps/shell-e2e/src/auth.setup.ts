import { test as setup, expect } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';

const AUTH_STATE_PATH = 'src/.auth/user.json';

// Cold-start budget: federation init + APP_INITIALIZER (Keycloak discovery +
// language + theme) + vite on-demand compile of the /auth/* lazy chunk +
// navigation to Keycloak + token exchange + return redirect. The global 30s
// test timeout from playwright.config.ts is not enough on a CI runner that is
// also running the full Aspire stack plus 4 Angular dev servers.
setup.setTimeout(120_000);

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

  try {
    await page.locator('yn-root > *').first().waitFor({ timeout: 60_000 });
  } catch (err) {
    // Dump the current DOM + key window globals so we can figure out what
    // Angular is stuck on. Without this the screenshot is a blank page.
    const diag = await page.evaluate(() => {
      const root = document.querySelector('yn-root');
      return {
        url: window.location.href,
        readyState: document.readyState,
        rootOuter: root?.outerHTML?.slice(0, 500),
        rootChildren: root?.childElementCount ?? 0,
        bodyText: document.body.innerText.slice(0, 200),
        ngExists: Reflect.has(window, 'ng') ? 'yes' : 'no',
      };
    });
    console.log('DIAG', JSON.stringify(diag));
    throw err;
  }

  // 2. Click sign in
  await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible({ timeout: 30_000 });
  await page.getByRole('button', { name: /sign in/i }).click();

  // 3. Wait for and fill Keycloak form
  await page.waitForURL('**/realms/**', { timeout: 15_000 });
  await page.locator('#username').fill(E2E_USER);
  await page.locator('#password').fill(E2E_PASSWORD);
  await page.locator('#kc-login').click();

  // 4. Wait for redirect — code exchange happens automatically
  await page.waitForURL('http://localhost:4200/**', { timeout: 15_000 });
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
