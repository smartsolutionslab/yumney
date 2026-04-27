import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe Tags (US-070)', () => {
  const recipe = setupSharedRecipe(test, 'E2E Tag Test Recipe', {
    ingredient: 'Flour',
    step: 'Mix',
    tags: ['italian', 'pasta', 'quick'],
  });

  test('should display tags on recipe detail page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto(`/recipes/${recipe().identifier}`);
    const detailPage = new RecipeDetailPage(authenticatedPage);
    await expect(detailPage.title).toBeVisible({ timeout: TIMEOUTS.default });

    const tags = authenticatedPage.locator('.tag');
    await expect(tags).toHaveCount(3);
  });

  test('should display tag text correctly', async ({ authenticatedPage }) => {
    await authenticatedPage.goto(`/recipes/${recipe().identifier}`);
    await expect(authenticatedPage.locator('.tag').first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const tagTexts = await authenticatedPage.locator('.tag').allTextContents();
    expect(tagTexts).toContain('italian');
    expect(tagTexts).toContain('pasta');
    expect(tagTexts).toContain('quick');
  });
});
