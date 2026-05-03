import { type Page, type Locator } from '@playwright/test';

/**
 * Lists overview at /shopping/lists. Distinct from ShoppingMergedPage
 * (/shopping) and ShoppingDetailPage (/shopping/lists/{id}).
 */
export class ShoppingListsPage {
  readonly heading: Locator;
  readonly listCards: Locator;
  readonly emptyState: Locator;
  readonly loading: Locator;
  readonly listsShell: Locator;
  readonly anyListLink: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.listCards = page.locator('.list-card');
    this.emptyState = page.locator('.empty-state');
    this.loading = page.locator('.loading');
    this.listsShell = page.locator('.lists-grid, .empty-state, .loading');
    this.anyListLink = page.locator('.list-card, .list-item, a[href*="/lists/"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/shopping/lists');
  }
}
