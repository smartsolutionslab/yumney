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
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);
    await expect(detail.title).toBeVisible({ timeout: TIMEOUTS.default });

    await expect(detail.tags).toHaveCount(3);
  });

  test('should display tag text correctly', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);
    await expect(detail.tags.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const tagTexts = await detail.tags.allTextContents();
    expect(tagTexts).toContain('italian');
    expect(tagTexts).toContain('pasta');
    expect(tagTexts).toContain('quick');
  });
});
