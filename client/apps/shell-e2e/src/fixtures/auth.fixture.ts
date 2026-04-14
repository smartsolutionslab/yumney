import { test as base, type Page } from '@playwright/test';

/**
 * Test fixture that provides an authenticated page.
 * Storage state (tokens) is injected by the setup project
 * via Playwright's storageState config — no manual login needed.
 *
 * Tests using `authenticatedPage` get a page with valid Keycloak
 * tokens already in sessionStorage.
 */
export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ page }, use) => {
    // Navigate to trigger angular-oauth2-oidc to pick up stored tokens
    await page.goto('/dashboard');
    await page.waitForLoadState('load');
    await use(page);
  },
});

export { expect } from '@playwright/test';
