import { test, expect } from '../fixtures/auth.fixture';
import { ShoppingCreatePage } from '../pages/shopping-create.page';
import { ShoppingDetailPage } from '../pages/shopping-detail.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Error-path coverage for the shopping-list detail page. Happy paths are
 * covered by shopping-list.spec.ts / shopping-mode.spec.ts; this spec
 * exercises optimistic-update rollback when the server rejects a mutation.
 *
 * The check-off mutation is wrapped in `optimisticSignalUpdate` so the UI
 * flips immediately and reverts on API failure. Without an E2E for the
 * revert branch, a future regression that drops the rollback step would
 * silently leak through (the optimistic flip alone would still look right
 * in passing happy-path tests).
 */
test.describe('Shopping List Detail — optimistic rollback', () => {
  const recipe = setupSharedRecipe(test, 'E2E Rollback', { ingredient: 'Flour' });

  test('reverts checkbox state when check-off API returns 500', async ({ authenticatedPage }) => {
    // Set up the route mock BEFORE creating the list so the very first
    // user click (post-navigation) hits the failure path. Cross-origin
    // gateway request — context.route, not page.route.
    await authenticatedPage.context().route('**/api/v1/shopping-lists/*/items/*/check*', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'about:blank',
          title: 'Internal Server Error',
          status: 500,
          detail: 'Synthetic failure injected by E2E to exercise the rollback path.',
        }),
      }),
    );

    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipe().identifier);
    await createPage.createButton.click();

    await expect(authenticatedPage).toHaveURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });
    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.heading).toBeVisible({ timeout: TIMEOUTS.default });

    const firstCheckbox = detailPage.itemCheckboxes.first();
    await expect(firstCheckbox).not.toBeChecked();

    await firstCheckbox.click();

    // After the optimistic flip + failed POST + rollback, the checkbox is
    // back to its original unchecked state. Allow a generous timeout — the
    // optimistic helper waits for the POST to settle before reverting.
    await expect(firstCheckbox).not.toBeChecked({ timeout: TIMEOUTS.default });
  });
});
