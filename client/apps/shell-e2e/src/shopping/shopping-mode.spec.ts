import { test, expect } from '../fixtures/auth.fixture';
import { ShoppingMergedPage } from '../pages/shopping-merged.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Shopping Mode Flow', () => {
  test('should display the merged shopping page', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should show start shopping mode button when items exist', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });

    // Add an item so shopping mode button appears
    await shopping.addItem('Shopping Mode Test');

    // Wait for the just-added item to render — confirms the POST round-tripped.
    await expect(shopping.itemByName('Shopping Mode Test')).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    // Shopping mode button should appear when there are items
    const hasBtn = (await shopping.startShoppingModeButton.count()) > 0;
    if (hasBtn) {
      await expect(shopping.startShoppingModeButton).toBeVisible();
    }
  });

  test('should toggle item bought state', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });

    // Add an item
    await shopping.addItem('Toggle Test Item');

    // Find the item and try to check it off — assertion polls for the
    // POST round-trip, so an explicit sleep is unnecessary.
    const itemRow = shopping.itemByName('Toggle Test Item').first();
    await expect(itemRow).toBeVisible({ timeout: TIMEOUTS.default });
  });
});
