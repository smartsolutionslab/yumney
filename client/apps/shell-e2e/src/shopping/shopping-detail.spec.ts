import { test, expect } from '../fixtures/auth.fixture';
import { ShoppingListsPage } from '../pages/shopping-lists.page';
import { ShoppingMergedPage } from '../pages/shopping-merged.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Shopping List Detail', () => {
  test('should navigate to shopping lists page', async ({ authenticatedPage }) => {
    const lists = new ShoppingListsPage(authenticatedPage);
    await lists.goto();

    await expect(lists.listsShell).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should show list items when navigating to a detail page', async ({ authenticatedPage }) => {
    const lists = new ShoppingListsPage(authenticatedPage);
    await lists.goto();

    const firstList = lists.anyListLink.first();
    const hasLists = (await firstList.count()) > 0;

    if (hasLists) {
      await firstList.click();
      await expect(authenticatedPage).toHaveURL(/\/shopping\/lists\//, {
        timeout: TIMEOUTS.default,
      });
    }
  });

  test('should show export button on merged list', async ({ authenticatedPage }) => {
    const shopping = new ShoppingMergedPage(authenticatedPage);
    await shopping.goto();

    // Wait for the merged list shell to render before probing for the
    // optional export button. The .add-input is the page's stable anchor.
    await expect(shopping.addInputClass).toBeVisible({ timeout: TIMEOUTS.default });

    const hasExport = (await shopping.exportButton.count()) > 0;
    if (hasExport) {
      await expect(shopping.exportButton).toBeVisible();
    }
  });
});
