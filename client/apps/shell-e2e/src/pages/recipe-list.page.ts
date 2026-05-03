import { type Page, type Locator } from '@playwright/test';

export class RecipeListPage {
  readonly heading: Locator;
  readonly searchInput: Locator;
  readonly searchClearButton: Locator;
  readonly sortToggle: Locator;
  readonly recipeCards: Locator;
  readonly emptyState: Locator;
  readonly ctaButton: Locator;
  readonly errorBanner: Locator;
  readonly loading: Locator;
  readonly filterToggle: Locator;
  readonly favoritesFilterChip: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.searchInput = page.locator('.search-input');
    this.searchClearButton = page.locator('.search-clear');
    this.sortToggle = page.locator('[data-testid="sort-toggle"]');
    this.recipeCards = page.locator('.recipe-card');
    this.emptyState = page.locator('.empty-state');
    this.ctaButton = page.locator('.cta-button');
    this.errorBanner = page.locator('[role="alert"]');
    this.loading = page.locator('.loading');
    this.filterToggle = page.locator('[data-testid="recipe-list-filter-toggle"]');
    this.favoritesFilterChip = page.locator('yn-filter-panel .filter-chip', {
      hasText: /show only favorites|nur favoriten/i,
    });
  }

  async goto(): Promise<void> {
    await this.page.goto('/recipes');
  }

  recipeCard(title: string): Locator {
    return this.recipeCards.filter({ hasText: title });
  }

  favoriteButtonOnCard(title: string): Locator {
    return this.recipeCard(title).locator('.favorite-overlay .favorite-button');
  }

  /**
   * Open the sort dropdown and pick an option by its sort value (e.g.
   * 'name-asc'). Mirrors the old `sortSelect.selectOption(value)` API.
   */
  async chooseSortOption(value: string): Promise<void> {
    await this.sortToggle.click();
    await this.page.locator(`[data-testid="sort-option-${value}"]`).click();
  }

  /** Currently-selected sort value, exposed via data-current-value on the toggle. */
  currentSortValue(): Promise<string | null> {
    return this.sortToggle.getAttribute('data-current-value');
  }
}
