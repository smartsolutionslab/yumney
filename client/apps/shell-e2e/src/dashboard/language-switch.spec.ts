import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { HeaderPage } from '../pages/header.page';

test.describe('Language Switching (US-060)', () => {
  let dashboard: DashboardPage;
  let header: HeaderPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    dashboard = new DashboardPage(authenticatedPage);
    header = new HeaderPage(authenticatedPage);
    await dashboard.goto();
  });

  test('should display language toggle button', async () => {
    await expect(header.langToggle).toBeVisible();
  });

  test('should switch from EN to DE', async () => {
    await expect(header.langToggle).toContainText('DE');

    await header.langToggle.click();
    await expect(header.langToggle).toContainText('EN');
  });

  test('should translate UI after switching language', async () => {
    await header.langToggle.click();

    await expect(header.logoutButton).toContainText('Abmelden');
  });

  test('should persist language after page reload', async ({ authenticatedPage }) => {
    await header.langToggle.click();
    await expect(header.langToggle).toContainText('EN');

    await authenticatedPage.reload();
    await expect(header.langToggle).toContainText('EN');
  });
});
