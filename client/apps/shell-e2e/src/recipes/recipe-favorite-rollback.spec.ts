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

  // Re-enabled — using context.route instead of page.route so the
  // mock intercepts cross-origin requests (page is on :4200, fetch
  // targets :5100 via apiBaseInterceptor). Page-scoped routes don't
  // catch those; context-scoped do. See #442.
  test('reverts aria-pressed when the favorite POST fails', async ({ authenticatedPage }) => {
    // Delay the rejected response so the optimistic flip is observable
    // before the rollback. 800ms is enough for Playwright's auto-retry
    // toHaveAttribute to land on the 'true' state at least once.
    // Regex pattern matches the full URL after gateway rewrite — gives a
    // clearer failure mode than a glob if the pattern slips. Scoped to
    // this recipe's identifier so other recipes' favorites still hit
    // the real backend.
    // Use a URL-predicate function so route matching is fully explicit —
    // earlier glob and regex attempts produced the same "route never fired"
    // failure mode. Log the match decision so CI traces show whether the
    // handler ran or the request hit the real backend.
    const targetIdentifier = recipe().identifier;
    let handlerFireCount = 0;
    // context.route, not page.route — see #442. The frontend rewrites
    // /api/v1/* to ${gatewayUrl}/api/v1/* via apiBaseInterceptor, so
    // these requests cross from :4200 to :5100 and page-scoped routes
    // don't intercept them.
    await authenticatedPage.context().route(
      (url) =>
        url.pathname === `/api/v1/recipes/${targetIdentifier}/favorite` ||
        url.pathname.endsWith(`/api/v1/recipes/${targetIdentifier}/favorite`),
      async (route) => {
        if (route.request().method() !== 'POST') {
          return route.continue();
        }
        handlerFireCount += 1;
        // eslint-disable-next-line no-console
        console.log(`[#409 mock] intercepted POST ${route.request().url()}`);
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
    const response = await favoritePost;
    // Diagnostic: confirm the response was actually our mock.
    // eslint-disable-next-line no-console
    console.log(
      `[#409 mock] favorite response status=${response.status()} handlerFireCount=${handlerFireCount}`,
    );
    expect(handlerFireCount, 'route handler should have intercepted the POST').toBeGreaterThan(0);
    expect(response.status(), 'mocked response should be 500').toBe(500);

    await expect(heart).toHaveAttribute('aria-pressed', 'false', { timeout: TIMEOUTS.default });
  });
});
