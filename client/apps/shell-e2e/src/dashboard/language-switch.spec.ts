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

  test('should display language switch in user menu', async () => {
    await header.openMenu();
    await expect(header.langSwitch).toBeVisible();
  });

  test('should switch from EN to DE', async () => {
    await header.openMenu();
    await expect(header.langSwitch).toHaveAttribute('data-current-lang', 'en');

    await header.switchLanguage();
    await header.openMenu();
    await expect(header.langSwitch).toHaveAttribute('data-current-lang', 'de');
  });

  test('should translate UI after switching language', async () => {
    await header.switchLanguage();

    // Logout label is one of the items that re-renders on lang change.
    await header.openMenu();
    await expect(header.logoutButton).toContainText('Abmelden');
  });

  test('should persist language after page reload', async ({ authenticatedPage }) => {
    await header.switchLanguage();
    await header.openMenu();
    await expect(header.langSwitch).toHaveAttribute('data-current-lang', 'de');

    await authenticatedPage.reload();

    await header.openMenu();
    await expect(header.langSwitch).toHaveAttribute('data-current-lang', 'de');
  });
});
