import { test, expect } from '../fixtures/auth.fixture';
import { MealPlannerPage } from '../pages/meal-planner.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Meal Planner Interactions', () => {
  test('should show clear button on hover when slot has meal', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const hasMeals = (await planner.mealTitles.count()) > 0;
    if (hasMeals) {
      const mealCard = planner.dayCardsWithMeal.first();
      await mealCard.hover();

      const clearBtn = planner.clearButtonOnCard(mealCard);
      await expect(clearBtn).toBeVisible({ timeout: TIMEOUTS.short });
    }
  });

  test('should display meal state badges for cooked/skipped', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    // If any slots have been confirmed, they should show state badges
    const count = await planner.mealStateBadges.count();
    if (count > 0) {
      const badge = planner.mealStateBadges.first();
      const text = await badge.textContent();
      expect(['Cooked', 'Skipped']).toContain(text?.trim());
    }
  });

  test('should show servings count for recipe slots', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const count = await planner.mealServingsLabels.count();
    if (count > 0) {
      const text = await planner.mealServingsLabels.first().textContent();
      expect(text).toMatch(/\d+.*servings/i);
    }
  });

  test('should show freetext label for freetext slots', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const count = await planner.mealFreetextSlots.count();
    if (count > 0) {
      await expect(planner.mealFreetextSlots.first()).toBeVisible();
    }
  });

  test('should show empty slot with plus icon for unassigned days', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const count = await planner.emptySlots.count();
    if (count > 0) {
      await expect(planner.emptySlots.first()).toBeVisible();
    }
  });

  test('should maintain state after week navigation', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();
    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });

    const originalWeek = (await planner.weekLabel.textContent()) ?? '';

    await planner.navNext.click();
    // Confirm we actually moved off the original week before navigating back.
    await expect(planner.weekLabel).not.toHaveText(originalWeek, { timeout: TIMEOUTS.default });

    await planner.navPrev.click();
    // Polling assertion replaces the prior read-once equality check.
    await expect(planner.weekLabel).toHaveText(originalWeek, { timeout: TIMEOUTS.default });
  });
});
