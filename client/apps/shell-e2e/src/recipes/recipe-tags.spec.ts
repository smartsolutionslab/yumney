import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';

test.describe('Recipe Tags (US-070)', () => {
  let recipeIdentifier: string;

  // Create a recipe with tags via API
  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();

    await page.goto('/auth/login');
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/realms/yumney/**');
    await page.locator('#username').fill('testuser');
    await page.locator('#password').fill('Test1234');
    await page.locator('#kc-login').click();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });

    // Create recipe with tags via API
    const response = await page.evaluate(async () => {
      const res = await fetch('/api/v1/recipes', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: 'E2E Tag Test Recipe',
          description: null,
          ingredients: [{ name: 'Flour', amount: 500, unit: 'g' }],
          steps: [{ number: 1, description: 'Mix' }],
          servings: 4,
          prepTimeMinutes: null,
          cookTimeMinutes: null,
          difficulty: null,
          imageUrl: null,
          tags: ['italian', 'pasta', 'quick'],
        }),
      });
      return res.json();
    });

    recipeIdentifier = response.identifier;
    await page.close();
  });

  test('should display tags on recipe detail page', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    await authenticatedPage.goto(`/recipes/${recipeIdentifier}`);
    const detailPage = new RecipeDetailPage(authenticatedPage);
    await expect(detailPage.heading).toBeVisible({ timeout: 10_000 });

    const tags = authenticatedPage.locator('.tag');
    await expect(tags).toHaveCount(3);
  });

  test('should display tag text correctly', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    await authenticatedPage.goto(`/recipes/${recipeIdentifier}`);
    await expect(authenticatedPage.locator('.tag').first()).toBeVisible({ timeout: 10_000 });

    const tagTexts = await authenticatedPage.locator('.tag').allTextContents();
    expect(tagTexts).toContain('italian');
    expect(tagTexts).toContain('pasta');
    expect(tagTexts).toContain('quick');
  });

  // Cleanup
  test.afterAll(async ({ browser }) => {
    if (!recipeIdentifier) return;

    const page = await browser.newPage();
    await page.goto('/auth/login');
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/realms/yumney/**');
    await page.locator('#username').fill('testuser');
    await page.locator('#password').fill('Test1234');
    await page.locator('#kc-login').click();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });

    await page.goto(`/recipes/${recipeIdentifier}`);
    await page.locator('.btn-danger').click();
    await page.locator('.btn-danger-filled').click();
    await page.waitForURL(/\/recipes$/, { timeout: 10_000 });
    await page.close();
  });
});
