import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';

test.describe('Language Switching (US-060)', () => {
  let dashboard: DashboardPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
    await dashboard.goto();
  });

  test('should display language toggle button', async ({ authenticatedPage }) => {
    const langToggle = authenticatedPage.locator('.lang-toggle');
    await expect(langToggle).toBeVisible();
  });

  test('should switch from EN to DE', async ({ authenticatedPage }) => {
    const langToggle = authenticatedPage.locator('.lang-toggle');
    await expect(langToggle).toContainText('DE');

    await langToggle.click();
    await expect(langToggle).toContainText('EN');
  });

  test('should translate UI after switching language', async ({ authenticatedPage }) => {
    const langToggle = authenticatedPage.locator('.lang-toggle');
    await langToggle.click();

    // German text should appear — check a known element
    await expect(authenticatedPage.locator('.logout-button')).toContainText('Abmelden');
  });

  test('should persist language after page reload', async ({ authenticatedPage }) => {
    const langToggle = authenticatedPage.locator('.lang-toggle');
    await langToggle.click();
    await expect(langToggle).toContainText('EN');

    await authenticatedPage.reload();
    await expect(authenticatedPage.locator('.lang-toggle')).toContainText('EN');
  });
});
