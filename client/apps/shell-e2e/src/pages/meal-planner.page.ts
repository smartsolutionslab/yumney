import { type Page, type Locator } from '@playwright/test';

export class MealPlannerPage {
  readonly heading: Locator;
  readonly weekLabel: Locator;
  readonly dayCards: Locator;
  readonly dayCardsWithMeal: Locator;
  readonly todayCard: Locator;
  readonly emptySlots: Locator;
  readonly mealTitles: Locator;
  readonly mealStateBadges: Locator;
  readonly mealServingsLabels: Locator;
  readonly mealFreetextSlots: Locator;
  readonly navPrev: Locator;
  readonly navNext: Locator;
  readonly generateButton: Locator;
  readonly plannerActions: Locator;
  readonly shoppingResult: Locator;
  readonly loading: Locator;
  readonly error: Locator;
  readonly retryButton: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.weekLabel = page.locator('.planner-header h1');
    this.dayCards = page.locator('.day-card');
    this.dayCardsWithMeal = page.locator('.day-card.has-meal');
    this.todayCard = page.locator('.day-card.today');
    this.emptySlots = page.locator('.empty-slot');
    this.mealTitles = page.locator('.meal-title');
    this.mealStateBadges = page.locator('.meal-state');
    this.mealServingsLabels = page.locator('.meal-servings');
    this.mealFreetextSlots = page.locator('.meal-freetext');
    this.navPrev = page.locator('[data-testid="meal-planner-nav-prev"]');
    this.navNext = page.locator('[data-testid="meal-planner-nav-next"]');
    this.generateButton = page.locator('.generate-btn');
    this.plannerActions = page.locator('.planner-actions');
    this.shoppingResult = page.locator('.shopping-result');
    this.loading = page.locator('.loading');
    this.error = page.locator('.error');
    this.retryButton = page.locator('.retry-btn');
  }

  async goto(): Promise<void> {
    await this.page.goto('/meal-planner');
  }

  dayCard(dayName: string): Locator {
    return this.dayCards.filter({ hasText: dayName });
  }

  mealTitle(dayName: string): Locator {
    return this.dayCard(dayName).locator('.meal-title');
  }

  clearButtonOnCard(card: Locator): Locator {
    return card.locator('.clear-btn');
  }
}
