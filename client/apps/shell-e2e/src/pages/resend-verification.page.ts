import { type Page, type Locator } from '@playwright/test';

export class ResendVerificationPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly emailInput: Locator;
  readonly submitButton: Locator;
  readonly successHeading: Locator;
  readonly successMessage: Locator;
  readonly backToRegisterLink: Locator;
  readonly errorBanner: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.subtitle = page.locator('.subtitle');
    this.emailInput = page.locator('#email');
    this.submitButton = page.getByRole('button', { name: /resend verification/i });
    this.successHeading = page.getByRole('heading', { name: /email sent/i });
    this.successMessage = page.locator('.success-message p');
    this.backToRegisterLink = page.getByRole('link', { name: /back to registration/i });
    this.errorBanner = page.locator('[role="alert"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/auth/resend-verification');
  }

  fieldError(text: string | RegExp): Locator {
    return this.page.locator('.field-error', { hasText: text });
  }
}
