import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { mockImportResponse, mockSavedRecipeResponse } from '../helpers/test-data.helper';

test.describe('Dashboard — Recipe Import (US-010, US-011, US-012, US-013)', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
  });

  test('should display URL input and import button', async ({ authenticatedPage }) => {
    await dashboard.goto();
    await expect(dashboard.urlInput).toBeVisible();
    await expect(dashboard.importButton).toBeVisible();
  });

  test('should show validation error on empty URL submission', async ({ authenticatedPage }) => {
    await dashboard.goto();
    await dashboard.importButton.click();

    await expect(dashboard.fieldError(/URL.*required|Please enter a URL/i)).toBeVisible();
  });

  test('should show validation error for invalid URL', async ({ authenticatedPage }) => {
    await dashboard.goto();
    await dashboard.urlInput.fill('not-a-url');
    await dashboard.importButton.click();

    await expect(dashboard.fieldError(/valid.*URL|invalid/i)).toBeVisible();
  });

  test('should show extracted recipe preview after successful import', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import', (route) =>
      route.fulfill({ status: 200, json: mockImportResponse }),
    );

    await dashboard.goto();
    await dashboard.urlInput.fill('https://example.com/recipe/carbonara');
    await dashboard.importButton.click();

    await expect(dashboard.recipePreview).toBeVisible();
  });

  test('should show error when page is unreachable', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import', (route) =>
      route.fulfill({ status: 502, json: { detail: 'Could not reach website' } }),
    );

    await dashboard.goto();
    await dashboard.urlInput.fill('https://unreachable.example.com/recipe');
    await dashboard.importButton.click();

    await expect(dashboard.errorBanner).toBeVisible();
  });

  test('should show error when no recipe found on page', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import', (route) =>
      route.fulfill({ status: 422, json: { detail: 'No recipe found' } }),
    );

    await dashboard.goto();
    await dashboard.urlInput.fill('https://example.com/not-a-recipe');
    await dashboard.importButton.click();

    await expect(dashboard.errorBanner).toBeVisible();
  });

  test('should save imported recipe and show success message', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import', (route) =>
      route.fulfill({ status: 200, json: mockImportResponse }),
    );
    await authenticatedPage.route('**/api/v1/recipes', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: mockSavedRecipeResponse });
      }
      return route.continue();
    });

    await dashboard.goto();
    await dashboard.urlInput.fill('https://example.com/recipe/carbonara');
    await dashboard.importButton.click();

    await expect(dashboard.recipePreview).toBeVisible();

    const saveButton = authenticatedPage.getByRole('button', { name: /save/i });
    await saveButton.click();

    await expect(dashboard.successBanner).toBeVisible();
    await expect(dashboard.successBanner).toContainText('Pasta Carbonara');
  });

  test('should show duplicate warning when recipe already imported', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import', (route) =>
      route.fulfill({ status: 200, json: mockImportResponse }),
    );
    await authenticatedPage.route('**/api/v1/recipes', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 409, json: { detail: 'Already imported' } });
      }
      return route.continue();
    });

    await dashboard.goto();
    await dashboard.urlInput.fill('https://example.com/recipe/carbonara');
    await dashboard.importButton.click();
    await expect(dashboard.recipePreview).toBeVisible();

    const saveButton = authenticatedPage.getByRole('button', { name: /save/i });
    await saveButton.click();

    await expect(dashboard.errorBanner).toBeVisible();
    await expect(dashboard.errorBanner).toContainText(/already/i);
  });

  test('should discard extracted recipe on discard click', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/import', (route) =>
      route.fulfill({ status: 200, json: mockImportResponse }),
    );

    await dashboard.goto();
    await dashboard.urlInput.fill('https://example.com/recipe');
    await dashboard.importButton.click();
    await expect(dashboard.recipePreview).toBeVisible();

    const discardButton = authenticatedPage.getByRole('button', { name: /discard/i });
    await discardButton.click();

    await expect(dashboard.recipePreview).not.toBeVisible();
  });
});

test.describe('Dashboard — Manual Recipe Entry (US-020)', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
  });

  test('should show create recipe button', async ({ authenticatedPage }) => {
    await dashboard.goto();
    await expect(dashboard.createButton).toBeVisible();
  });

  test('should show empty recipe preview on create click', async ({ authenticatedPage }) => {
    await dashboard.goto();
    await dashboard.createButton.click();

    await expect(dashboard.recipePreview).toBeVisible();
  });
});
