import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';

test.describe('Dashboard — Share Intent (US-124)', () => {
  test('should populate URL field from ?url query param', async ({ authenticatedPage }) => {
    const dashboard = new DashboardPage(authenticatedPage);
    await authenticatedPage.goto('/dashboard?url=https://example.com/recipe');

    await expect(dashboard.urlInput).toHaveValue('https://example.com/recipe');
  });

  test('should auto-start import when ?url is provided', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import/stream*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: 'event: status\ndata: Fetching page...\n\n',
      }),
    );

    await authenticatedPage.goto('/dashboard?url=https://example.com/recipe');

    await expect(authenticatedPage.getByText(/fetching|extracting|loading/i)).toBeVisible({
      timeout: 5_000,
    });
  });

  test('should extract URL from ?text query param', async ({ authenticatedPage }) => {
    const dashboard = new DashboardPage(authenticatedPage);
    await authenticatedPage.goto('/dashboard?text=Check+this+recipe+https://example.com/recipe');

    await expect(dashboard.urlInput).toHaveValue('https://example.com/recipe');
  });

  test('should not auto-import when no query params', async ({ authenticatedPage }) => {
    const dashboard = new DashboardPage(authenticatedPage);
    await dashboard.goto();

    await expect(dashboard.urlInput).toHaveValue('');
    await expect(authenticatedPage.getByText(/fetching|extracting/i)).not.toBeVisible();
  });

  test('should not auto-import when ?text has no URL', async ({ authenticatedPage }) => {
    const dashboard = new DashboardPage(authenticatedPage);
    await authenticatedPage.goto('/dashboard?text=just+some+text');

    await expect(dashboard.urlInput).toHaveValue('');
  });
});
