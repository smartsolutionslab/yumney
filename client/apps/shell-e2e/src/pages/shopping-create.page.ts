import { type Page, type Locator } from '@playwright/test';

export class ShoppingCreatePage {
  readonly heading: Locator;
  readonly titleInput: Locator;
  readonly ingredientCheckboxes: Locator;
  readonly selectAllButton: Locator;
  readonly deselectAllButton: Locator;
  readonly createButton: Locator;
  readonly errorBanner: Locator;
  readonly backLink: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.titleInput = page.locator('#shopping-list-title');
    this.ingredientCheckboxes = page.locator('input[type="checkbox"]');
    // Exact match — case-insensitive /select all/i also matches "Deselect all"
    // and triggers Playwright's strict-mode violation.
    this.selectAllButton = page.getByRole('button', { name: 'Select all', exact: true });
    this.deselectAllButton = page.getByRole('button', { name: 'Deselect all', exact: true });
    this.createButton = page.locator('.create-button');
    this.errorBanner = page.locator('[role="alert"]');
    this.backLink = page.locator('.back-link');
  }

  async goto(recipeIdentifier: string): Promise<void> {
    await this.page.goto(`/shopping/create/${recipeIdentifier}`);
  }
}
