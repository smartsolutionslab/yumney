import { type Page, type Locator, expect } from '@playwright/test';

/**
 * Wraps the header avatar dropdown which now hosts the language and logout
 * controls (was a flat .lang-toggle / .logout-button before the redesign).
 */
export class HeaderPage {
  readonly userMenuToggle: Locator;
  readonly langSwitch: Locator;
  readonly logoutButton: Locator;

  constructor(private page: Page) {
    this.userMenuToggle = page.locator('[data-testid="user-menu-toggle"]');
    this.langSwitch = page.locator('[data-testid="lang-switch"]');
    this.logoutButton = page.locator('[data-testid="logout"]');
  }

  navLink(label: string | RegExp): Locator {
    return this.page.locator('.nav-link').filter({ hasText: label });
  }

  /** Open the avatar dropdown menu. Idempotent. */
  async openMenu(): Promise<void> {
    if (await this.langSwitch.isVisible()) return;
    await this.userMenuToggle.click();
    await expect(this.langSwitch).toBeVisible();
  }

  /** Open menu, click the language switch item, then return the new active lang. */
  async switchLanguage(): Promise<void> {
    await this.openMenu();
    await this.langSwitch.click();
    // Click closes the dropdown. Caller should re-openMenu() if they need to
    // assert against the new state.
  }

  async logout(): Promise<void> {
    await this.userMenuToggle.click();
    await this.logoutButton.click();
  }
}
