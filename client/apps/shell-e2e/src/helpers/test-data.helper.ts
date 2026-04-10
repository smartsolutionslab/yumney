import { type Page, expect } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { SELECTORS } from './selectors';
import { TIMEOUTS } from './timeouts';

/**
 * Test constants for E2E tests against the real system.
 * The test user is pre-seeded in the Keycloak realm import.
 */
export const TEST_USER = {
  username: process.env['E2E_USER'] ?? 'testuser',
  password: process.env['E2E_PASSWORD'] ?? 'Test1234',
  email: 'test@yumney.dev',
  displayName: 'Test User',
};

export function uniqueTitle(prefix: string): string {
  return `${prefix} ${Date.now()}`;
}

export function uniqueEmail(prefix: string = 'e2e'): string {
  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2)}@yumney.dev`;
}

/**
 * Authenticate via the Keycloak login form.
 * Use this in beforeAll blocks where the auth fixture is not available.
 */
export async function loginViaKeycloak(
  page: Page,
  username: string = TEST_USER.username,
  password: string = TEST_USER.password,
): Promise<void> {
  await page.goto('/auth/login');
  await page.getByRole('button', { name: /sign in/i }).click();
  await page.waitForURL('**/realms/yumney/protocol/openid-connect/**');
  await page.locator(SELECTORS.keycloak.username).fill(username);
  await page.locator(SELECTORS.keycloak.password).fill(password);
  await page.locator(SELECTORS.keycloak.loginBtn).click();
  await page.waitForURL('**/dashboard', { timeout: TIMEOUTS.long });
}

/**
 * Create a basic recipe via the manual entry form and return its identifier.
 */
export async function createTestRecipe(
  page: Page,
  title: string,
  options?: { ingredient?: string; step?: string; servings?: number },
): Promise<string> {
  const dashboard = new DashboardPage(page);
  await dashboard.createButton.click();

  await page.locator('#title').fill(title);

  const ingredientName = page.locator(`${SELECTORS.form.ingredients} input[type="text"]`).first();
  await ingredientName.fill(options?.ingredient ?? 'Test Ingredient');

  const stepDescription = page.locator(`${SELECTORS.form.steps} textarea`).first();
  await stepDescription.fill(options?.step ?? 'Test Step');

  if (options?.servings) {
    const servingsInput = page.locator('#servings');
    await servingsInput.clear();
    await servingsInput.fill(String(options.servings));
  }

  await page.getByRole('button', { name: /save/i }).click();
  await expect(page.locator(SELECTORS.banners.success)).toBeVisible({
    timeout: TIMEOUTS.long,
  });

  await page.goto('/recipes');
  await page.waitForTimeout(1000);

  const card = page.locator(SELECTORS.recipe.card).filter({ hasText: title }).first();
  const href = await card.getAttribute('href');
  return href?.replace('/recipes/', '') ?? '';
}

/**
 * Delete a recipe by navigating to its detail page and confirming deletion.
 */
export async function deleteTestRecipe(page: Page, recipeIdentifier: string): Promise<void> {
  await page.goto(`/recipes/${recipeIdentifier}`);
  await page.locator(SELECTORS.recipe.deleteBtn).click();
  await page.locator(SELECTORS.recipe.confirmDelete).click();
  await page.waitForURL(/\/recipes$/, { timeout: TIMEOUTS.default });
}
