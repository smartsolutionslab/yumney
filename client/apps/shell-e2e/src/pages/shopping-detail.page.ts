import { type Page, type Locator } from '@playwright/test';

export class ShoppingDetailPage {
  readonly heading: Locator;
  readonly itemCheckboxes: Locator;
  readonly items: Locator;
  readonly checkedItems: Locator;
  readonly checkAllButton: Locator;
  readonly resetButton: Locator;
  readonly progress: Locator;
  readonly backLink: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.itemCheckboxes = page.locator('.items-list input[type="checkbox"]');
    this.items = page.locator('.items-list li');
    this.checkedItems = page.locator('.items-list li.checked');
    this.checkAllButton = page.getByRole('button', { name: /check all/i });
    this.resetButton = page.getByRole('button', { name: /reset/i });
    this.progress = page.locator('.progress');
    this.backLink = page.locator('.back-link');
  }

  async goto(identifier: string): Promise<void> {
    await this.page.goto(`/shopping/${identifier}`);
  }
}
