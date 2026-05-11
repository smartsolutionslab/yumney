import { test, expect } from '../fixtures/auth.fixture';
import { MealAnalyticsPage } from '../pages/meal-analytics.page';
import { MealPlannerPage } from '../pages/meal-planner.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Meal Analytics (US-332)', () => {
  test('loads the analytics page with the period toolbar', async ({ authenticatedPage }) => {
    const analytics = new MealAnalyticsPage(authenticatedPage);
    await analytics.goto();

    await expect(analytics.heading).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(analytics.monthToggle).toBeVisible();
    await expect(analytics.yearToggle).toBeVisible();
    await expect(analytics.periodLabel).toBeVisible();
  });

  test('period label matches the monthly format by default', async ({ authenticatedPage }) => {
    const analytics = new MealAnalyticsPage(authenticatedPage);
    await analytics.goto();

    await expect(analytics.periodLabel).toBeVisible({ timeout: TIMEOUTS.default });
    const label = await analytics.periodLabel.textContent();
    expect(label).toMatch(/^\d{4}-\d{2}$/);
  });

  test('switching to yearly view changes the period format', async ({ authenticatedPage }) => {
    const analytics = new MealAnalyticsPage(authenticatedPage);
    await analytics.goto();

    await expect(analytics.periodLabel).toBeVisible({ timeout: TIMEOUTS.default });
    await analytics.yearToggle.click();

    // Polling assertion waits for Angular change detection to propagate the
    // viewMode flip into the rendered DOM; a one-shot textContent() read can
    // beat the update.
    await expect(analytics.periodLabel).toHaveText(/^\d{4}$/);
  });

  test('next-period advances the label', async ({ authenticatedPage }) => {
    const analytics = new MealAnalyticsPage(authenticatedPage);
    await analytics.goto();

    await expect(analytics.periodLabel).toBeVisible({ timeout: TIMEOUTS.default });
    const before = await analytics.periodLabel.textContent();
    await analytics.nextPeriod.click();
    await expect(analytics.periodLabel).not.toHaveText(before ?? '');
  });

  test('back link returns to the meal planner', async ({ authenticatedPage }) => {
    const analytics = new MealAnalyticsPage(authenticatedPage);
    const planner = new MealPlannerPage(authenticatedPage);
    await analytics.goto();

    await expect(analytics.back).toBeVisible({ timeout: TIMEOUTS.default });
    await analytics.back.click();

    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('opening analytics from the meal-planner header works', async ({ authenticatedPage }) => {
    const planner = new MealPlannerPage(authenticatedPage);
    const analytics = new MealAnalyticsPage(authenticatedPage);
    await planner.goto();

    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
    const openAnalytics = authenticatedPage.locator('[data-testid="meal-planner-open-analytics"]');
    await expect(openAnalytics).toBeVisible();
    await openAnalytics.click();

    await expect(analytics.heading).toBeVisible({ timeout: TIMEOUTS.default });
  });
});
