import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { loginViaKeycloak, deleteTestRecipe } from '../helpers/test-data.helper';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe Tags (US-070)', () => {
  let recipeIdentifier: string;

  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();
    await loginViaKeycloak(page);

    // Create recipe with tags via API (authenticated session from loginViaKeycloak)
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
    await expect(detailPage.title).toBeVisible({ timeout: TIMEOUTS.default });

    const tags = authenticatedPage.locator('.tag');
    await expect(tags).toHaveCount(3);
  });

  test('should display tag text correctly', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    await authenticatedPage.goto(`/recipes/${recipeIdentifier}`);
    await expect(authenticatedPage.locator('.tag').first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const tagTexts = await authenticatedPage.locator('.tag').allTextContents();
    expect(tagTexts).toContain('italian');
    expect(tagTexts).toContain('pasta');
    expect(tagTexts).toContain('quick');
  });

  test.afterAll(async ({ browser }) => {
    if (!recipeIdentifier) return;

    const page = await browser.newPage();
    await loginViaKeycloak(page);
    await deleteTestRecipe(page, recipeIdentifier);
    await page.close();
  });
});
