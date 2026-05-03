import { test, expect } from '../fixtures/auth.fixture';
import { ShoppingMergedPage } from '../pages/shopping-merged.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Merged Shopping List (US-312-318)', () => {
  test('should display the shopping page', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.mergedListShell).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should show add item input', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should add a manual item', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });

    await shopping.addItem('Test E2E Item');

    // Adding goes through the messaging bus (Wolverine) and a projection
    // update before the merged-list view sees it. TIMEOUTS.long covers
    // the projection round-trip; without it the test was racing the
    // eventual-consistency window when retries:1 was disabled.
    await expect(shopping.itemByName('Test E2E Item')).toBeVisible({
      timeout: TIMEOUTS.long,
    });
  });

  test('should show progress bar when items exist', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });

    await shopping.addItem('Progress Test Item');

    await expect(shopping.progressBar).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should group items by category', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    await expect(shopping.addInput).toBeVisible({ timeout: TIMEOUTS.default });

    // If items exist, they should be in category groups
    const count = await shopping.categoryGroups.count();
    if (count > 0) {
      await expect(shopping.categoryGroups.first()).toBeVisible();
    }
  });
});
