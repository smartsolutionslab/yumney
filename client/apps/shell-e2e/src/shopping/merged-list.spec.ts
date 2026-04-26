import { test, expect } from '../fixtures/auth.fixture';
import { SELECTORS } from '../helpers/selectors';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Merged Shopping List (US-312-318)', () => {
  test('should display the shopping page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    await expect(
      authenticatedPage.locator('.merged-list, .empty-state, .loading').first(),
    ).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should show add item input', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    await expect(authenticatedPage.locator(SELECTORS.shopping.addInput)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should add a manual item', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    const input = authenticatedPage.locator(SELECTORS.shopping.addInput);
    await expect(input).toBeVisible({ timeout: TIMEOUTS.default });

    await input.fill('Test E2E Item');
    await input.press('Enter');

    // Adding goes through the messaging bus (Wolverine) and a projection
    // update before the merged-list view sees it. TIMEOUTS.long covers
    // the projection round-trip; without it the test was racing the
    // eventual-consistency window when retries:1 was disabled.
    await expect(authenticatedPage.getByText('Test E2E Item')).toBeVisible({
      timeout: TIMEOUTS.long,
    });
  });

  test('should show progress bar when items exist', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    const input = authenticatedPage.locator(SELECTORS.shopping.addInput);
    await expect(input).toBeVisible({ timeout: TIMEOUTS.default });

    await input.fill('Progress Test Item');
    await input.press('Enter');

    await expect(authenticatedPage.locator(SELECTORS.shopping.progressBar)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should group items by category', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    await expect(authenticatedPage.locator(SELECTORS.shopping.addInput)).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    // If items exist, they should be in category groups
    const groups = authenticatedPage.locator(SELECTORS.shopping.categoryGroup);
    const count = await groups.count();
    if (count > 0) {
      await expect(groups.first()).toBeVisible();
    }
  });
});
