import { type Page, type Locator } from '@playwright/test';

export class DashboardPage {
  readonly heading: Locator;
  readonly importToggle: Locator;
  readonly urlInput: Locator;
  readonly importButton: Locator;
  readonly createButton: Locator;
  readonly photoUploadInput: Locator;
  readonly photoUploadLabel: Locator;
  readonly recipePreview: Locator;
  readonly successBanner: Locator;
  readonly errorBanner: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.importToggle = page.locator('[data-testid="import-toggle"]');
    this.urlInput = page.locator('#url');
    this.importButton = page.getByRole('button', { name: /import recipe/i });
    this.createButton = page.locator('[data-testid="create-recipe-btn"]');
    this.photoUploadInput = page.locator('[data-testid="photo-upload-input"]');
    this.photoUploadLabel = page.locator('[data-testid="photo-upload-btn"]');
    this.recipePreview = page.locator('yn-recipe-preview');
    this.successBanner = page.locator('.success-banner');
    this.errorBanner = page.locator('[role="alert"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/dashboard');
    // The import section is collapsed by default after the dashboard
    // redesign. All import-related controls are hidden until expanded, so
    // every E2E test that touches them needs to expand first.
    await this.expandImportSection();
  }

  async expandImportSection(): Promise<void> {
    const expanded = await this.importToggle.getAttribute('data-expanded');
    if (expanded !== 'true') {
      await this.importToggle.click();
    }
  }

  fieldError(text: string | RegExp): Locator {
    return this.page.locator('.field-error', { hasText: text });
  }
}
