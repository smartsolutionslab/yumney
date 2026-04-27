import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
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

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
    await dashboard.goto();
  });

  test('shows error banner when save returns 500', async ({ authenticatedPage }) => {
    await mockApiError(authenticatedPage, '**/api/v1/recipes', 500, {
      detail: 'An unexpected error occurred.',
    });

    await dashboard.createButton.click();
    await expect(dashboard.recipePreview).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(authenticatedPage.locator('#preview-title')).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    await fillMinimalRecipeForm(authenticatedPage, 'E2E 500 Test');

    // Scope save-btn to within the recipe-preview so we don't accidentally
    // hit another .save-btn elsewhere on the dashboard. Wait for the
    // POST response before asserting the banner so we know the click
    // actually fired the request the mock is supposed to fulfill.
    const savePost = authenticatedPage.waitForResponse(
      (res) =>
        new URL(res.url()).pathname === '/api/v1/recipes' &&
        res.request().method() === 'POST',
      { timeout: TIMEOUTS.default },
    );
    await dashboard.recipePreview.locator('.save-btn').click();
    await savePost;

    await expect(dashboard.errorBanner).toBeVisible({ timeout: TIMEOUTS.default });
    // Form must survive the error so the user can retry without retyping.
    await expect(authenticatedPage.locator('#preview-title')).toHaveValue('E2E 500 Test');
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
    await expect(authenticatedPage.locator('#preview-title')).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    await fillMinimalRecipeForm(authenticatedPage, 'E2E 422 Test');

    const savePost = authenticatedPage.waitForResponse(
      (res) =>
        new URL(res.url()).pathname === '/api/v1/recipes' &&
        res.request().method() === 'POST',
      { timeout: TIMEOUTS.default },
    );
    await dashboard.recipePreview.locator('.save-btn').click();
    await savePost;

    await expect(dashboard.errorBanner).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(authenticatedPage.locator('#preview-title')).toHaveValue('E2E 422 Test');
  });
});

async function fillMinimalRecipeForm(
  page: import('@playwright/test').Page,
  title: string,
): Promise<void> {
  // The manual-create flow pre-populates one empty ingredient row and one
  // empty step row; only need to fill them so client-side validation
  // doesn't block the submit. Ingredient amount is optional per the form,
  // but the name is not.
  await page.locator('#preview-title').fill(title);
  await page.locator('#preview-servings').fill('4');
  // First ingredient + step rows exist by default; their inputs use
  // formControlName="name" / "description" within their row containers.
  await page.locator('input[formControlName="name"]').first().fill('Salt');
  await page.locator('textarea[formControlName="description"]').first().fill('Mix everything.');
}
