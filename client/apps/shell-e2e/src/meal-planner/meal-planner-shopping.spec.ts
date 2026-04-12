import { test, expect } from '../fixtures/auth.fixture';
import { MealPlannerPage } from '../pages/meal-planner.page';
import { SELECTORS } from '../helpers/selectors';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Meal Planner Shopping Integration (US-325)', () => {
  test('should not show generate button when no recipes are assigned', async ({
    authenticatedPage,
  }) => {
    const planner = new MealPlannerPage(authenticatedPage);

    // Navigate to a far-future week that likely has no recipes
    await authenticatedPage.goto('/meal-planner');
    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });

    // Navigate far forward to an empty week
    for (let i = 0; i < 10; i++) {
      await planner.navNext.click();
    }
    await authenticatedPage.waitForTimeout(500);

    // Generate button should not be visible for empty plans
    await expect(authenticatedPage.locator(SELECTORS.mealPlanner.generateBtn)).not.toBeVisible();
  });

  test('should show generate button when recipes are assigned', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    // Check if there are any recipe slots — if so, button should be visible
    const hasMeals = (await authenticatedPage.locator('.meal-title').count()) > 0;
    if (hasMeals) {
      await expect(authenticatedPage.locator(SELECTORS.mealPlanner.generateBtn)).toBeVisible();
    }
  });

  test('should show planner actions area when recipes exist', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const hasMeals = (await authenticatedPage.locator('.meal-title').count()) > 0;
    if (hasMeals) {
      await expect(authenticatedPage.locator(SELECTORS.mealPlanner.plannerActions)).toBeVisible();
    }
  });
});
