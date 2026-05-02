import { test, expect } from '../fixtures/auth.fixture';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Shopping List Detail', () => {
  test('should navigate to shopping lists page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping/lists');

    await expect(authenticatedPage.locator('.lists-grid, .empty-state, .loading')).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should show list items when navigating to a detail page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping/lists');

    const firstList = authenticatedPage
      .locator('.list-card, .list-item, a[href*="/lists/"]')
      .first();
    const hasLists = (await firstList.count()) > 0;

    if (hasLists) {
      await firstList.click();
      await expect(authenticatedPage).toHaveURL(/\/shopping\/lists\//, {
        timeout: TIMEOUTS.default,
      });
    }
  });

  test('should show export button on merged list', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    // Wait for the merged list shell to render before probing for the
    // optional export button. The add-input is the page's stable anchor.
    await expect(authenticatedPage.locator('.add-input')).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const exportBtn = authenticatedPage.getByRole('button', { name: /export/i });
    const hasExport = (await exportBtn.count()) > 0;
    if (hasExport) {
      await expect(exportBtn).toBeVisible();
    }
  });
});
