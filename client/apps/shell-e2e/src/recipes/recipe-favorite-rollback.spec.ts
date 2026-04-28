import { test, expect } from '../fixtures/auth.fixture';
import { RecipeListPage } from '../pages/recipe-list.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Coverage for #409 — optimistic-update rollback on the favorite toggle.
 *
 * `toggleFavoriteInList` (libs/shared/models/src/lib/favorite-toggle.ts)
 * flips the recipe's isFavorite signal synchronously, then fires
 * POST /api/v1/recipes/{id}/favorite. On success it reconciles with the
 * server response; on error it reverts to the original value. Without
 * this revert path, an offline / 5xx user would think their action
 * succeeded and only discover otherwise on next reload.
 *
 * The route handler delays the rejection response so the test has a
 * deterministic window to observe the optimistic state before the
 * rollback fires.
 */
test.describe('Favorite Recipes — Optimistic Rollback (#409)', () => {
  const recipe = setupSharedRecipe(test, 'E2E Favorite Rollback', {
    ingredient: 'Pepper',
  });

  test('reverts aria-pressed when the favorite POST fails', async ({ authenticatedPage }) => {
    // Delay the rejected response so the optimistic flip is observable
    // before the rollback. 800ms is enough for Playwright's auto-retry
    // toHaveAttribute to land on the 'true' state at least once.
    await authenticatedPage.route(
      `**/api/v1/recipes/${recipe().identifier}/favorite`,
      async (route) => {
        if (route.request().method() !== 'POST') {
          return route.continue();
        }
        await new Promise((resolve) => setTimeout(resolve, 800));
        return route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            type: 'https://tools.ietf.org/html/rfc7231#section-500',
            title: 'Internal Server Error',
            status: 500,
          }),
        });
      },
    );

    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const heart = list.favoriteButtonOnCard(recipe().title).first();
    await expect(heart).toHaveAttribute('aria-pressed', 'false');

    // Wait for the (failed) POST to complete before asserting the revert.
    const favoritePost = authenticatedPage.waitForResponse(
      (res) =>
        new RegExp(`/api/v1/recipes/${recipe().identifier}/favorite$`).test(res.url()) &&
        res.request().method() === 'POST',
      { timeout: TIMEOUTS.default },
    );
    await heart.click();

    // Optimistic flip: aria-pressed should read 'true' within the window
    // before the route handler's 800ms delay completes. toHaveAttribute
    // polls; if the flip happened we'll catch it, even if briefly.
    await expect(heart).toHaveAttribute('aria-pressed', 'true', { timeout: TIMEOUTS.short });

    // Wait for the server response (500), then verify the rollback.
    await favoritePost;
    await expect(heart).toHaveAttribute('aria-pressed', 'false', { timeout: TIMEOUTS.default });
  });
});
