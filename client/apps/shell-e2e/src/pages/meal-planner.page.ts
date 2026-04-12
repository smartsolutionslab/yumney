import { type Page, type Locator } from '@playwright/test';
import { SELECTORS } from '../helpers/selectors';

export class MealPlannerPage {
  readonly dayCards: Locator;
  readonly emptySlots: Locator;
  readonly weekLabel: Locator;
  readonly navPrev: Locator;
  readonly navNext: Locator;
  readonly loading: Locator;
  readonly error: Locator;
  readonly retryBtn: Locator;

  constructor(private page: Page) {
    this.dayCards = page.locator(SELECTORS.mealPlanner.dayCard);
    this.emptySlots = page.locator(SELECTORS.mealPlanner.emptySlot);
    this.weekLabel = page.locator(SELECTORS.mealPlanner.weekLabel);
    this.navPrev = page.locator(SELECTORS.mealPlanner.navPrev);
    this.navNext = page.locator(SELECTORS.mealPlanner.navNext);
    this.loading = page.locator(SELECTORS.mealPlanner.loading);
    this.error = page.locator(SELECTORS.mealPlanner.error);
    this.retryBtn = page.locator(SELECTORS.mealPlanner.retryBtn);
  }

  async goto(): Promise<void> {
    await this.page.goto('/meal-planner');
  }

  dayCard(dayName: string): Locator {
    return this.page.locator(SELECTORS.mealPlanner.dayCard).filter({ hasText: dayName });
  }

  mealTitle(dayName: string): Locator {
    return this.dayCard(dayName).locator(SELECTORS.mealPlanner.mealTitle);
  }
}
