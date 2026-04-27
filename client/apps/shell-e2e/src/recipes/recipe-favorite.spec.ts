import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { RecipeListPage } from '../pages/recipe-list.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Favorite Recipes (US-071)', () => {
  const recipe = setupSharedRecipe(test, 'E2E Favorite', {
    ingredient: 'Salt',
    step: 'Add salt to taste',
  });

  test('should toggle favorite from recipe list card', async ({ authenticatedPage }) => {
    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const heart = list.favoriteButtonOnCard(recipe().title).first();
    await expect(heart).toHaveAttribute('aria-pressed', 'false');

    await heart.click();
    await expect(heart).toHaveAttribute('aria-pressed', 'true');
  });

  test('should persist favorite state across reload', async ({ authenticatedPage }) => {
    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const heart = list.favoriteButtonOnCard(recipe().title).first();
    await expect(heart).toHaveAttribute('aria-pressed', 'true', { timeout: TIMEOUTS.short });

    await authenticatedPage.reload();
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await expect(heart).toHaveAttribute('aria-pressed', 'true');
  });

  test('should narrow list when "Show only favorites" filter is enabled', async ({
    authenticatedPage,
  }) => {
    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    await list.filterToggle.click();
    await list.favoritesFilterChip.click();

    // Filter triggers a fresh GET with favoritesOnly=true; with retries:0
    // the previous toggle's POST may not have committed before the filter
    // refetch fires. Use the long timeout to absorb that.
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.long,
    });
  });

  test('should reflect favorite state on recipe detail page', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await expect(detail.favoriteButton).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(detail.favoriteButton).toHaveAttribute('aria-pressed', 'true');
  });

  test('should toggle favorite back off from recipe detail', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);
    await expect(detail.favoriteButton).toBeVisible({ timeout: TIMEOUTS.default });

    await detail.favoriteButton.click();
    await expect(detail.favoriteButton).toHaveAttribute('aria-pressed', 'false');
  });
});
