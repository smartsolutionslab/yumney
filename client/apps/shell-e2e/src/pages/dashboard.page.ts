import { type Page, type Locator } from '@playwright/test';

export class DashboardPage {
  readonly heading: Locator;
  readonly urlInput: Locator;
  readonly importButton: Locator;
  readonly createButton: Locator;
  readonly recipePreview: Locator;
  readonly successBanner: Locator;
  readonly errorBanner: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.urlInput = page.locator('#url');
    this.importButton = page.getByRole('button', { name: /import recipe/i });
    this.createButton = page.getByRole('button', { name: /create recipe/i });
    this.recipePreview = page.locator('yn-recipe-preview');
    this.successBanner = page.locator('.success-banner');
    this.errorBanner = page.locator('[role="alert"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/dashboard');
  }

  fieldError(text: string | RegExp): Locator {
    return this.page.locator('.field-error', { hasText: text });
  }
}
