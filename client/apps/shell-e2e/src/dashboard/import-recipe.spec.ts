import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';

test.describe('Dashboard — Recipe Import (US-010, US-011, US-012, US-013)', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
    await dashboard.goto();
  });

  test('should display URL input and import button', async () => {
    await expect(dashboard.urlInput).toBeVisible();
    await expect(dashboard.importButton).toBeVisible();
  });

  test('should show validation error on empty URL submission', async () => {
    await dashboard.importButton.click();
    await expect(dashboard.fieldError(/required|Please enter a URL/i)).toBeVisible();
  });

  test('should show validation error for invalid URL', async () => {
    await dashboard.urlInput.fill('not-a-url');
    await dashboard.importButton.click();
    await expect(dashboard.fieldError(/valid.*URL|invalid/i)).toBeVisible();
  });

  test('should show error for unreachable URL', async ({ authenticatedPage }) => {
    // Mock the SSE import stream to fail immediately. Without this we'd hit
    // the real backend, which uses AddStandardResilienceHandler — DNS
    // failures retry with exponential backoff for ~40-50s on busy CI runners,
    // making this the flakiest test in the suite (#420). What we actually
    // want to verify is the *UI behaviour* on a failed import, which a mock
    // covers cleanly. The real resilience path stays covered by backend
    // integration tests.
    await authenticatedPage.route('**/api/v1/recipes/import/stream**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        headers: { 'Cache-Control': 'no-cache' },
        body: 'event: fail\ndata: Could not reach URL\n\n',
      }),
    );

    await dashboard.urlInput.fill('https://this-domain-does-not-exist-e2e.invalid/recipe');
    await dashboard.importButton.click();

    // 90s — belt-and-braces. If the mock fires the banner appears in <1s.
    // If something layers above page.route (NGSW intercept post-#424,
    // service-worker fetch handling, etc.) and the request reaches the
    // real backend, the frontend's SSE_TIMEOUT_MS = 60_000 in
    // recipe-api.service.ts surfaces a Connection-lost error within
    // 60-70s. The 90s budget covers both paths under CI load.
    await expect(dashboard.errorBanner).toBeVisible({ timeout: 90_000 });
  });

  test('should display create recipe button', async () => {
    await expect(dashboard.createButton).toBeVisible();
  });

  test('should show empty recipe preview on manual create click', async () => {
    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible();
  });

  test('should discard manual recipe preview', async ({ authenticatedPage }) => {
    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible();

    const discardButton = authenticatedPage.getByRole('button', { name: /discard/i });
    await discardButton.click();

    await expect(dashboard.recipePreview).not.toBeVisible();
  });
});

test.describe('Dashboard — Photo Import (US-130)', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
    await dashboard.goto();
  });

  test('should display photo upload section', async () => {
    await expect(dashboard.photoUploadLabel).toBeVisible();
  });

  test('should have file input that accepts images', async () => {
    await expect(dashboard.photoUploadInput).toHaveAttribute('accept', /image/);
    await expect(dashboard.photoUploadInput).toHaveAttribute('multiple', '');
  });

  // Camera capture is handled by a dedicated `Scan with camera` button now,
  // not by a `capture="environment"` attribute on the file input. The button
  // is only rendered when the browser exposes a camera API, which Playwright
  // headless Chromium doesn't — so we just assert the file input still works.
});
