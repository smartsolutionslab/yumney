import { type Page, type Locator } from '@playwright/test';

/**
 * Single shopping list at /shopping/lists/{id}. Distinct from
 * ShoppingMergedPage (/shopping) and ShoppingListsPage (/shopping/lists).
 */
export class ShoppingDetailPage {
  readonly heading: Locator;
  readonly itemCheckboxes: Locator;
  readonly items: Locator;
  readonly checkedItems: Locator;
  readonly checkAllButton: Locator;
  readonly resetButton: Locator;
  readonly progress: Locator;
  readonly backLink: Locator;
  readonly addInput: Locator;
  readonly exportButton: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.itemCheckboxes = page.locator('.items-list input[type="checkbox"]');
    this.items = page.locator('.items-list li');
    this.checkedItems = page.locator('.items-list li.checked');
    this.checkAllButton = page.getByRole('button', { name: /check all/i });
    this.resetButton = page.getByRole('button', { name: /reset/i });
    this.progress = page.locator('.progress-text');
    this.backLink = page.locator('.back-link');
    this.addInput = page.locator('.add-input');
    this.exportButton = page.getByRole('button', { name: /export/i });
  }

  async goto(identifier: string): Promise<void> {
    await this.page.goto(`/shopping/lists/${identifier}`);
  }
}
