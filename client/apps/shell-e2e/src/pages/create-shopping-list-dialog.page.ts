import { type Page, type Locator } from '@playwright/test';

/**
 * Dialog opened from the recipe detail page when creating a shopping
 * list (US-080). Lives as its own object because the dialog is a
 * sibling DOM tree under [data-testid="create-shopping-list-dialog"]
 * and several specs interact with its inner controls.
 */
export class CreateShoppingListDialogPage {
  readonly root: Locator;
  readonly suggestedTitle: Locator;
  readonly previewItems: Locator;
  readonly confirmButton: Locator;

  constructor(private page: Page) {
    this.root = page.locator('[data-testid="create-shopping-list-dialog"]');
    this.suggestedTitle = this.root.locator('[data-testid="create-shopping-list-suggested-title"]');
    this.previewItems = this.root.locator('.preview-list li');
    this.confirmButton = page.locator('[data-testid="create-shopping-list-confirm"]');
  }
}
