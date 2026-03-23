import { type Page, type Locator } from '@playwright/test';

export class LoginPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly rememberMeCheckbox: Locator;
  readonly signInButton: Locator;
  readonly registerLink: Locator;
  readonly forgotPasswordLink: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.subtitle = page.locator('.subtitle');
    this.rememberMeCheckbox = page.getByRole('checkbox');
    this.signInButton = page.getByRole('button', { name: /sign in/i });
    this.registerLink = page.getByRole('link', { name: /create one/i });
    this.forgotPasswordLink = page.getByRole('button', { name: /forgot your password/i });
  }

  async goto(): Promise<void> {
    await this.page.goto('/auth/login');
  }
}
