import { type Page, type Locator } from '@playwright/test';

export class RecipeDetailPage {
  readonly title: Locator;
  readonly description: Locator;
  readonly ingredients: Locator;
  readonly steps: Locator;
  readonly servingsValue: Locator;
  readonly increaseServingsButton: Locator;
  readonly decreaseServingsButton: Locator;
  readonly resetServingsButton: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;
  readonly shoppingListButton: Locator;
  readonly sourceLink: Locator;
  readonly confirmDialog: Locator;
  readonly errorBanner: Locator;
  readonly backLink: Locator;
  readonly favoriteButton: Locator;

  constructor(private page: Page) {
    this.title = page.locator('.recipe-title');
    this.description = page.locator('.recipe-description');
    this.ingredients = page.locator('.ingredients-list li');
    this.steps = page.locator('.steps-list li');
    this.servingsValue = page.locator('.servings-value');
    this.increaseServingsButton = page.getByLabel(/increase servings/i);
    this.decreaseServingsButton = page.getByLabel(/decrease servings/i);
    this.resetServingsButton = page.getByRole('button', { name: /reset/i });
    this.editButton = page.getByRole('link', { name: /edit/i });
    this.deleteButton = page.locator('.btn-danger');
    // Action button on the recipe-detail page. Exact match to avoid colliding
    // with the header nav link "Shopping Lists".
    this.shoppingListButton = page.getByRole('link', { name: 'Shopping List', exact: true });
    this.sourceLink = page.locator('.source-link a');
    // Target the .confirm-overlay child rather than the host yn-confirm-dialog.
    // The host has 0x0 dimensions because its child is position:fixed, so
    // Playwright reports the host as hidden even when the dialog is visible.
    this.confirmDialog = page.locator('yn-confirm-dialog .confirm-overlay');
    this.errorBanner = page.locator('[role="alert"]');
    this.backLink = page.locator('.back-link');
    this.favoriteButton = page.locator('.actions-bar yn-favorite-button .favorite-button');
  }

  async goto(identifier: string): Promise<void> {
    await this.page.goto(`/recipes/${identifier}`);
  }
}
