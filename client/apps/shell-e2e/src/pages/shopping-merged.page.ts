import { type Page, type Locator } from '@playwright/test';

/**
 * Merged shopping list at /shopping — the unified, cross-list view.
 * Distinct from ShoppingDetailPage (single list, /shopping/lists/{id}).
 */
export class ShoppingMergedPage {
  readonly heading: Locator;
  readonly addInput: Locator;
  readonly addInputClass: Locator;
  readonly progressBar: Locator;
  readonly categoryGroups: Locator;
  readonly emptyState: Locator;
  readonly retryButton: Locator;
  readonly mergedListShell: Locator;
  readonly startShoppingModeButton: Locator;
  readonly exportButton: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.addInput = page.locator('[data-testid="shopping-add-input"]');
    // The class-based selector — the merged-list page renders the same
    // input with .add-input. Kept distinct from addInput because the
    // shopping detail page also uses .add-input but lacks the testid.
    this.addInputClass = page.locator('.add-input');
    this.progressBar = page.locator('[data-testid="shopping-progress-bar"]');
    this.categoryGroups = page.locator('[data-testid="shopping-category-group"]');
    this.emptyState = page.locator('[data-testid="shopping-empty-state"]');
    this.retryButton = page.locator('.retry-btn');
    this.mergedListShell = page.locator('.merged-list, .empty-state, .loading').first();
    this.startShoppingModeButton = page.getByRole('button', { name: /shopping mode|start/i });
    this.exportButton = page.getByRole('button', { name: /export/i });
  }

  async goto(): Promise<void> {
    await this.page.goto('/shopping');
  }

  async addItem(name: string): Promise<void> {
    await this.addInput.fill(name);
    await this.addInput.press('Enter');
  }

  itemByName(name: string): Locator {
    return this.page.getByText(name);
  }
}
