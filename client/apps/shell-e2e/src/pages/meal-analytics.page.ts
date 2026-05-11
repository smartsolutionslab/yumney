import { type Page, type Locator } from '@playwright/test';

export class MealAnalyticsPage {
  readonly heading: Locator;
  readonly back: Locator;
  readonly monthToggle: Locator;
  readonly yearToggle: Locator;
  readonly prevPeriod: Locator;
  readonly nextPeriod: Locator;
  readonly periodLabel: Locator;
  readonly totalCooked: Locator;
  readonly distribution: Locator;
  readonly topRecipes: Locator;
  readonly empty: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.back = page.locator('[data-testid="meal-analytics-back"]');
    this.monthToggle = page.locator('[data-testid="meal-analytics-view-month"]');
    this.yearToggle = page.locator('[data-testid="meal-analytics-view-year"]');
    this.prevPeriod = page.locator('[data-testid="meal-analytics-prev"]');
    this.nextPeriod = page.locator('[data-testid="meal-analytics-next"]');
    this.periodLabel = page.locator('[data-testid="meal-analytics-period"]');
    this.totalCooked = page.locator('[data-testid="meal-analytics-total-cooked"]');
    this.distribution = page.locator('[data-testid="meal-analytics-distribution"]');
    this.topRecipes = page.locator('[data-testid="meal-analytics-top-recipes"]');
    this.empty = page.locator('[data-testid="meal-analytics-empty"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/meal-planner/analytics');
  }
}
