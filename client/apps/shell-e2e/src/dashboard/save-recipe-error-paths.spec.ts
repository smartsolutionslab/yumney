import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { RecipeEditPage } from '../pages/recipe-edit.page';
import { mockApiError } from '../helpers/error-mock';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Coverage for #408 — error paths on POST /api/v1/recipes (manual create
 * → save). Previously every spec exercised happy-path saves only; users
 * had no e2e safety net for what happens when the backend errors.
 *
 * Both tests use mockApiError to inject a ProblemDetails response and
 * assert against the dashboard's error banner ([role="alert"]). The
 * mock's body shape mirrors GlobalExceptionHandlerMiddleware /
 * ValidationExtensions output so the frontend's error-mapping sees
 * realistic input.
 */
test.describe('Dashboard — Save Recipe Error Paths (#408)', () => {
  let dashboard: DashboardPage;
  let editForm: RecipeEditPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
    editForm = new RecipeEditPage(authenticatedPage);
    await dashboard.goto();
  });

  // Re-enabled — mockApiError now uses context.route, which intercepts
  // cross-origin requests (page on :4200, fetch on :5100). See #442.
  test('shows error banner when save returns 500', async ({ authenticatedPage }) => {
    await mockApiError(authenticatedPage, '**/api/v1/recipes', 500, {
      detail: 'An unexpected error occurred.',
    });

    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(editForm.titleInput).toBeVisible({ timeout: TIMEOUTS.default });

    await editForm.fillMinimal('E2E 500 Test');

    // Wait for the POST response before asserting the banner so we know
    // the click actually fired the request the mock is supposed to fulfill.
    const savePost = authenticatedPage.waitForResponse(
      (res) =>
        new URL(res.url()).pathname === '/api/v1/recipes' && res.request().method() === 'POST',
      { timeout: TIMEOUTS.default },
    );
    await dashboard.previewSaveButton.click();
    await savePost;

    await expect(dashboard.errorBanner).toBeVisible({ timeout: TIMEOUTS.default });
    // Form must survive the error so the user can retry without retyping.
    await expect(editForm.titleInput).toHaveValue('E2E 500 Test');
  });

  test('shows error banner when save returns 422 with validation errors', async ({
    authenticatedPage,
  }) => {
    await mockApiError(authenticatedPage, '**/api/v1/recipes', 422, {
      errors: {
        title: ["'Title' must not be empty."],
        'ingredients[0].name': ["'Name' must not be empty."],
      },
    });

    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(editForm.titleInput).toBeVisible({ timeout: TIMEOUTS.default });

    await editForm.fillMinimal('E2E 422 Test');

    const savePost = authenticatedPage.waitForResponse(
      (res) =>
        new URL(res.url()).pathname === '/api/v1/recipes' && res.request().method() === 'POST',
      { timeout: TIMEOUTS.default },
    );
    await dashboard.previewSaveButton.click();
    await savePost;

    await expect(dashboard.errorBanner).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(editForm.titleInput).toHaveValue('E2E 422 Test');
  });
});
