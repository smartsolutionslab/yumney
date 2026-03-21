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
    this.selectAllButton = page.getByRole('button', { name: /select all/i });
    this.deselectAllButton = page.getByRole('button', { name: /deselect all/i });
    this.createButton = page.locator('.create-button');
    this.errorBanner = page.locator('[role="alert"]');
    this.backLink = page.locator('.back-link');
  }

  async goto(recipeIdentifier: string): Promise<void> {
    await this.page.goto(`/shopping/create/${recipeIdentifier}`);
  }
}
