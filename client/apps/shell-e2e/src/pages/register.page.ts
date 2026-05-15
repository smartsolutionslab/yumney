import { type Page, type Locator } from '@playwright/test';

export class RegisterPage {
  readonly heading: Locator;
  readonly subtitle: Locator;
  readonly emailInput: Locator;
  readonly displayNameInput: Locator;
  readonly passwordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly submitButton: Locator;
  readonly successHeading: Locator;
  readonly successMessage: Locator;
  readonly resendLink: Locator;
  readonly errorBanner: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.subtitle = page.locator('.subtitle');
    this.emailInput = page.locator('#email');
    this.displayNameInput = page.locator('#displayName');
    this.passwordInput = page.locator('#password');
    this.confirmPasswordInput = page.locator('#confirmPassword');
    this.submitButton = page.getByRole('button', { name: /create account/i });
    this.successHeading = page.getByRole('heading', { name: /check your email/i });
    this.successMessage = page.locator('.success-message p');
    this.resendLink = page.getByRole('link', { name: /resend/i });
    this.errorBanner = page.locator('[role="alert"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/auth/register');
  }

  async fillForm(data: { email: string; displayName: string; password: string; confirmPassword: string }): Promise<void> {
    await this.emailInput.fill(data.email);
    await this.displayNameInput.fill(data.displayName);
    await this.passwordInput.fill(data.password);
    await this.confirmPasswordInput.fill(data.confirmPassword);
  }

  fieldError(text: string | RegExp): Locator {
    return this.page.locator('.field-error', { hasText: text });
  }
}
