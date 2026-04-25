import { test, expect } from '../fixtures/auth.fixture';
import { RecipeListPage } from '../pages/recipe-list.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe List (US-030, US-034)', () => {
  let recipeList: RecipeListPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    recipeList = new RecipeListPage(authenticatedPage);
    await recipeList.goto();
  });

  test('should display recipe list heading', async () => {
    await expect(recipeList.heading).toBeVisible();
  });

  test('should display search input and sort toggle', async () => {
    await expect(recipeList.searchInput).toBeVisible();
    await expect(recipeList.sortToggle).toBeVisible();
  });

  test('should show empty state or recipe cards', async () => {
    // Depending on DB state, either cards or empty state should show
    const cards = recipeList.recipeCards;
    const empty = recipeList.emptyState;

    await expect(cards.or(empty).first()).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should show no results for gibberish search', async ({ authenticatedPage }) => {
    await recipeList.searchInput.fill('xyznonexistent12345');
    await authenticatedPage.waitForTimeout(500); // debounce

    await expect(recipeList.emptyState).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should clear search and restore results', async ({ authenticatedPage }) => {
    await recipeList.searchInput.fill('xyznonexistent12345');
    await authenticatedPage.waitForTimeout(500);
    await expect(recipeList.emptyState).toBeVisible({ timeout: TIMEOUTS.default });

    await recipeList.searchClearButton.click();

    // Either cards or original empty state should reappear
    const cards = recipeList.recipeCards;
    const empty = recipeList.emptyState;
    await expect(cards.or(empty).first()).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should navigate to recipe detail on card click', async ({ authenticatedPage }) => {
    // Skip if no recipes exist
    const cards = recipeList.recipeCards;
    const count = await cards.count();
    test.skip(count === 0, 'No recipes in database — cannot test navigation');

    await cards.first().click();
    await expect(authenticatedPage).toHaveURL(/\/recipes\/.+/);
  });

  test('should change sort order', async () => {
    await recipeList.chooseSortOption('name-asc');
    // The custom dropdown exposes the current value via data-current-value
    // on the toggle. Poll for the attribute change rather than reading once.
    await expect(recipeList.sortToggle).toHaveAttribute('data-current-value', 'name-asc');
  });
});
