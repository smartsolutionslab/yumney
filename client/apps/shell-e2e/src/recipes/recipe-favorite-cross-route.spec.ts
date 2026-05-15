import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { RecipeListPage } from '../pages/recipe-list.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Cross-route favorite consistency in the *reverse* direction of
 * recipe-favorite.spec.ts. That spec toggles from the list card and verifies
 * the detail page reflects it; this one does the inverse — toggle from the
 * detail page, then verify the list card shows the same state on a fresh
 * navigation. Without it, a regression that drops the list query's favorite
 * join (or that fails to refetch on route entry) would slip through, because
 * the existing spec only exercises the list→detail axis.
 */
test.describe('Favorite cross-route consistency (US-071)', () => {
  test.describe.configure({ mode: 'serial' });

  const recipe = setupSharedRecipe(test, 'E2E Favorite Cross-Route', {
    ingredient: 'Salt',
    step: 'Add salt to taste',
  });

  test('toggling favorite on detail propagates to the list card', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);
    await expect(detail.favoriteButton).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(detail.favoriteButton).toHaveAttribute('aria-pressed', 'false');

    // Wait on the POST commit before navigating away — without it the list
    // fetch can race the unfinished write and miss the new state (see #419
    // on the existing favorite spec).
    const favoriteCommitted = authenticatedPage.waitForResponse(
      (res) => /\/api\/v1\/recipes\/.+\/favorite$/.test(res.url()) && res.request().method() === 'POST' && res.ok(),
      { timeout: TIMEOUTS.default },
    );
    await detail.favoriteButton.click();
    await favoriteCommitted;

    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipe().title).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const heart = list.favoriteButtonOnCard(recipe().title).first();
    await expect(heart).toHaveAttribute('aria-pressed', 'true');
  });
});
