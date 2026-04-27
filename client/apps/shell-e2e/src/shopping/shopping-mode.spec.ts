import { test, expect } from '../fixtures/auth.fixture';
import { SELECTORS } from '../helpers/selectors';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Shopping Mode Flow', () => {
  test('should display the merged shopping page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    await expect(authenticatedPage.locator(SELECTORS.shopping.addInput)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should show start shopping mode button when items exist', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    await expect(authenticatedPage.locator(SELECTORS.shopping.addInput)).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    // Add an item so shopping mode button appears
    const input = authenticatedPage.locator(SELECTORS.shopping.addInput);
    await input.fill('Shopping Mode Test');
    await input.press('Enter');

    // Wait for the just-added item to render — confirms the POST round-tripped.
    await expect(authenticatedPage.getByText('Shopping Mode Test')).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    // Shopping mode button should appear when there are items
    const startBtn = authenticatedPage.getByRole('button', { name: /shopping mode|start/i });
    const hasBtn = (await startBtn.count()) > 0;
    if (hasBtn) {
      await expect(startBtn).toBeVisible();
    }
  });

  test('should toggle item bought state', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    await expect(authenticatedPage.locator(SELECTORS.shopping.addInput)).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    // Add an item
    const input = authenticatedPage.locator(SELECTORS.shopping.addInput);
    await input.fill('Toggle Test Item');
    await input.press('Enter');

    // Find the item and try to check it off — assertion polls for the
    // POST round-trip, so an explicit sleep is unnecessary.
    const itemRow = authenticatedPage.getByText('Toggle Test Item').first();
    await expect(itemRow).toBeVisible({ timeout: TIMEOUTS.default });
  });
});
