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

  // Re-enabled — #430 fix landed in recipe-api.service.ts: the SSE
  // timeout now calls subscriber.error directly instead of relying on
  // a catch guard that the abort-on-timeout silently bypassed.
  //
  // The original test pointed at a `.invalid` URL and relied on a real
  // DNS failure. That worked locally but flakes in CI where chromium's
  // resolver can hang for tens of seconds on non-existent TLDs. Instead,
  // intercept the import-stream and hang the response — the SSE timeout
  // (60s) is the actual #430 codepath under test, and a hung route
  // exercises it deterministically.
  test('should show error when SSE import times out', async ({ authenticatedPage }) => {
    test.setTimeout(120_000);
    await authenticatedPage.context().route('**/api/v1/recipes/import/stream*', () => {
      // Never call fulfill/continue/abort — the request hangs until
      // recipe-api.service.ts's 60s timeout fires, which is exactly the
      // path #430 made user-visible.
    });

    await dashboard.urlInput.fill('https://example.com/recipe');
    await dashboard.importButton.click();

    // SSE_TIMEOUT_MS is 60 s; allow some headroom for the catch / emit
    // chain. The banner appears as soon as subscriber.error fires.
    await expect(dashboard.errorBanner).toBeVisible({ timeout: 75_000 });
  });

  // Mid-import server failure: the gateway returns 422 with ProblemDetails
  // before the SSE stream starts (LLM extraction rejected the URL because the
  // page didn't contain a recipe). Verifies the error banner surfaces instead
  // of leaving the form spinning indefinitely.
  test('should show error banner when import stream returns 422', async ({ authenticatedPage }) => {
    await authenticatedPage.context().route('**/api/v1/recipes/import/stream*', (route) =>
      route.fulfill({
        status: 422,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'about:blank',
          title: 'Validation error(s) occurred.',
          status: 422,
          detail: 'No recipe could be extracted from the supplied URL.',
        }),
      }),
    );

    await dashboard.urlInput.fill('https://example.com/not-a-recipe');
    await dashboard.importButton.click();

    await expect(dashboard.errorBanner).toBeVisible();
  });

  // In-stream failure: the server starts the SSE response then emits a
  // `fail` event (LLM returned malformed JSON, or scraping hit a dead end
  // partway through). The UI must surface the banner and re-enable the form.
  test('should show error banner when SSE emits a fail event', async ({ authenticatedPage }) => {
    await authenticatedPage.context().route('**/api/v1/recipes/import/stream*', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: 'event: status\ndata: Starting extraction…\n\nevent: fail\ndata: extraction failed\n\n',
      }),
    );

    await dashboard.urlInput.fill('https://example.com/partial-recipe');
    await dashboard.importButton.click();

    await expect(dashboard.errorBanner).toBeVisible();
    await expect(dashboard.importButton).toBeEnabled();
  });

  test('should display create recipe button', async () => {
    await expect(dashboard.createButton).toBeVisible();
  });

  test('should show empty recipe preview on manual create click', async () => {
    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible();
  });

  test('should discard manual recipe preview', async () => {
    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible();

    await dashboard.discardButton.click();

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
