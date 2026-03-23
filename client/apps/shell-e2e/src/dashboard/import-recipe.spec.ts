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
    await dashboard.urlInput.fill('https://this-domain-does-not-exist-e2e.invalid/recipe');
    await dashboard.importButton.click();

    await expect(dashboard.errorBanner).toBeVisible({ timeout: 30_000 });
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

  test('should have camera capture attribute for mobile', async () => {
    await expect(dashboard.photoUploadInput).toHaveAttribute('capture', 'environment');
  });
});
