import { test as base, type Page } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';

/**
 * Authenticates against the real Keycloak instance by navigating to
 * the login page, clicking "Sign in with Keycloak", and completing
 * the Keycloak login form.
 *
 * Requires the full system running (Aspire AppHost):
 *   dotnet run --project src/Yumney.AppHost
 */
async function authenticateViaKeycloak(page: Page): Promise<void> {
  await page.goto('/auth/login');

  // Click the app's "Sign in with Keycloak" button
  await page.getByRole('button', { name: /sign in/i }).click();

  // Now on Keycloak login page — wait for it to load
  await page.waitForURL('**/realms/yumney/protocol/openid-connect/**');

  // Fill Keycloak credentials
  await page.locator('#username').fill(E2E_USER);
  await page.locator('#password').fill(E2E_PASSWORD);
  await page.locator('#kc-login').click();

  // Wait for redirect back to the app dashboard
  await page.waitForURL('**/dashboard', { timeout: 15_000 });
}

export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ page }, use) => {
    await authenticateViaKeycloak(page);
    await use(page);
  },
});

export { expect } from '@playwright/test';
