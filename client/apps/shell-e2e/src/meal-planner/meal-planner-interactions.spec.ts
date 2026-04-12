import { test, expect } from '../fixtures/auth.fixture';
import { MealPlannerPage } from '../pages/meal-planner.page';
import { SELECTORS } from '../helpers/selectors';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Meal Planner Interactions', () => {
  test('should show clear button on hover when slot has meal', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const hasMeals = (await authenticatedPage.locator('.meal-title').count()) > 0;
    if (hasMeals) {
      const mealCard = authenticatedPage.locator('.day-card.has-meal').first();
      await mealCard.hover();

      const clearBtn = mealCard.locator(SELECTORS.mealPlanner.clearBtn);
      await expect(clearBtn).toBeVisible({ timeout: TIMEOUTS.short });
    }
  });

  test('should display meal state badges for cooked/skipped', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    // If any slots have been confirmed, they should show state badges
    const stateBadges = authenticatedPage.locator(SELECTORS.mealPlanner.mealState);
    const count = await stateBadges.count();
    if (count > 0) {
      const badge = stateBadges.first();
      const text = await badge.textContent();
      expect(['Cooked', 'Skipped']).toContain(text?.trim());
    }
  });

  test('should show servings count for recipe slots', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const servingsLabels = authenticatedPage.locator(SELECTORS.mealPlanner.mealServings);
    const count = await servingsLabels.count();
    if (count > 0) {
      const text = await servingsLabels.first().textContent();
      expect(text).toMatch(/\d+.*servings/i);
    }
  });

  test('should show freetext label for freetext slots', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const freetextSlots = authenticatedPage.locator(SELECTORS.mealPlanner.mealFreetext);
    const count = await freetextSlots.count();
    if (count > 0) {
      await expect(freetextSlots.first()).toBeVisible();
    }
  });

  test('should show empty slot with plus icon for unassigned days', async ({
    authenticatedPage,
  }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const emptySlots = authenticatedPage.locator(SELECTORS.mealPlanner.emptySlot);
    const count = await emptySlots.count();
    if (count > 0) {
      await expect(emptySlots.first()).toBeVisible();
    }
  });

  test('should maintain state after week navigation', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });

    const originalWeek = await planner.weekLabel.textContent();

    await planner.navNext.click();
    await authenticatedPage.waitForTimeout(500);
    await planner.navPrev.click();
    await authenticatedPage.waitForTimeout(500);

    const restoredWeek = await planner.weekLabel.textContent();
    expect(restoredWeek).toBe(originalWeek);
  });
});
