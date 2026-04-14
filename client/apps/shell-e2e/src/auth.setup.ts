import { test as setup, expect } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';

const AUTH_STATE_PATH = 'src/.auth/user.json';

/**
 * Playwright setup: performs a real browser-based Keycloak login once
 * and saves the authenticated browser state (localStorage + cookies).
 * All test projects reuse this state — no login needed per test.
 */
setup('authenticate via Keycloak browser login', async ({ page }) => {
  // Navigate to the app login page
  await page.goto('/auth/login');
  await page.waitForLoadState('load');

  // Click the "Sign in with Keycloak" button
  const signInButton = page.getByRole('button', { name: /sign in/i });
  await expect(signInButton).toBeVisible({ timeout: 10_000 });
  await signInButton.click();

  // Wait for Keycloak login page
  await page.waitForURL('**/realms/yumney/**', { timeout: 15_000 });

  // Fill credentials on Keycloak form
  await page.locator('#username').fill(E2E_USER);
  await page.locator('#password').fill(E2E_PASSWORD);
  await page.locator('#kc-login').click();

  // Wait for redirect back to the app
  await page.waitForURL('**/dashboard', { timeout: 15_000 });

  // Ensure the app has fully initialized with the auth tokens
  await page.waitForLoadState('load');
  await page.waitForTimeout(2000);

  // Persist "remember me" so tokens go to localStorage (survives storageState)
  await page.evaluate(() => localStorage.setItem('yn_remember_me', 'true'));

  // Save the authenticated state
  await page.context().storageState({ path: AUTH_STATE_PATH });
});
