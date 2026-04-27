import { test, expect } from '../fixtures/auth.fixture';
import { MealPlannerPage } from '../pages/meal-planner.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Meal Planner (US-320)', () => {
  test('should display the meal planner page with 7 day cards', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(planner.dayCards).toHaveCount(7);
  });

  test('should display week label in ISO format', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
    const label = await planner.weekLabel.textContent();
    expect(label).toMatch(/\d{4}-W\d{2}/);
  });

  test('should show all 7 day names', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    for (const day of [
      'Monday',
      'Tuesday',
      'Wednesday',
      'Thursday',
      'Friday',
      'Saturday',
      'Sunday',
    ]) {
      await expect(planner.dayCard(day)).toBeVisible();
    }
  });

  test('should highlight today', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const todayCard = authenticatedPage.locator('.day-card.today');
    await expect(todayCard).toBeVisible();
  });

  test('should show empty slots for a new plan', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(planner.emptySlots).toHaveCount(7);
  });

  test('should navigate to next week', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
    const initialLabel = (await planner.weekLabel.textContent()) ?? '';

    await planner.navNext.click();
    // Polling assertion: passes once the label flips off the initial value.
    await expect(planner.weekLabel).not.toHaveText(initialLabel, { timeout: TIMEOUTS.default });
  });

  test('should navigate to previous week', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
    const initialLabel = (await planner.weekLabel.textContent()) ?? '';

    await planner.navPrev.click();
    await expect(planner.weekLabel).not.toHaveText(initialLabel, { timeout: TIMEOUTS.default });
  });

  test('should navigate to meal planner from header', async ({ authenticatedPage }) => {
    const navLink = authenticatedPage.locator('.nav-link').filter({ hasText: 'Meal Planner' });
    await expect(navLink).toBeVisible({ timeout: TIMEOUTS.default });

    await navLink.click();

    await expect(authenticatedPage).toHaveURL(/\/meal-planner/, { timeout: TIMEOUTS.default });
    await expect(authenticatedPage.locator('.day-card').first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should be responsive on mobile viewport', async ({ authenticatedPage }) => {
    await authenticatedPage.setViewportSize({ width: 375, height: 812 });
    const planner = new MealPlannerPage(authenticatedPage);
    await planner.goto();

    await expect(planner.dayCards.first()).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(planner.dayCards).toHaveCount(7);
  });
});
