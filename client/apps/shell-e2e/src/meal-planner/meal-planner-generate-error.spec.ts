import { test, expect } from '../fixtures/auth.fixture';
import { MealPlannerPage } from '../pages/meal-planner.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { assignMealSlot, openAuthenticatedPage } from '../helpers/test-data.helper';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Error-path coverage for the meal-planner "Generate Shopping List" flow.
 *
 * Happy-path is covered by meal-planner-shopping.spec.ts; this spec
 * exercises the failure branch (server returns 5xx). Without the test, a
 * regression that leaves the planner in an infinite spinner — or one that
 * silently swallows the error — would slip through.
 *
 * Seeds a recipe + a meal-plan slot via API in beforeAll so the generate
 * button is reliably visible, instead of relying on whatever happens to
 * be in the shared testuser plan today.
 */
test.describe('Meal Planner — generate shopping list error path', () => {
  const recipe = setupSharedRecipe(test, 'E2E GenerateError', { ingredient: 'Olive Oil' });

  // ISO week the test seeds + asserts against. Far-future so it doesn't
  // collide with happy-path specs that exercise the current week.
  const seededYear = 2030;
  const seededWeek = 15;

  test.beforeAll(async ({ browser }) => {
    const setupPage = await openAuthenticatedPage(browser);
    try {
      await assignMealSlot(setupPage, {
        year: seededYear,
        weekNumber: seededWeek,
        day: 'Monday',
        recipeIdentifier: recipe().identifier,
        recipeTitle: recipe().title,
      });
    } finally {
      await setupPage.context().close();
    }
  });

  test('surfaces server error when generate-shopping-list returns 500', async ({ authenticatedPage }) => {
    await authenticatedPage.context().route('**/api/v1/meal-plans/*/w/*/generate-shopping-list', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          type: 'about:blank',
          title: 'Internal Server Error',
          status: 500,
          detail: 'Synthetic failure injected by E2E to exercise the error path.',
        }),
      }),
    );

    await authenticatedPage.goto(`/meal-planner?year=${seededYear}&week=${seededWeek}`);

    const planner = new MealPlannerPage(authenticatedPage);
    await expect(planner.weekLabel).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(planner.generateButton).toBeVisible({ timeout: TIMEOUTS.default });

    await planner.generateButton.click();

    // The async-state contract: error becomes visible, loading clears, and
    // the planner stays interactive (no permanent spinner). Generate button
    // re-enables so the user can retry.
    await expect(planner.error).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(planner.generateButton).toBeEnabled();
  });
});
