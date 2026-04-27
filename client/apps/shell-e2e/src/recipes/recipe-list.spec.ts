import { test, expect } from '../fixtures/auth.fixture';
import { RecipeListPage } from '../pages/recipe-list.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe List (US-030, US-034)', () => {
  let recipeList: RecipeListPage;
  // Seed a known recipe so the "navigate on click" test isn't gated on
  // ambient DB state (previously skipped silently when DB was empty).
  const recipe = setupSharedRecipe(test, 'E2E List Test');

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

  test('should show no results for gibberish search', async () => {
    await recipeList.searchInput.fill('xyznonexistent12345');
    // Search input is debounced; the polling assertion below covers the wait.
    await expect(recipeList.emptyState).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should clear search and restore results', async () => {
    await recipeList.searchInput.fill('xyznonexistent12345');
    await expect(recipeList.emptyState).toBeVisible({ timeout: TIMEOUTS.default });

    await recipeList.searchClearButton.click();

    // Either cards or original empty state should reappear
    const cards = recipeList.recipeCards;
    const empty = recipeList.emptyState;
    await expect(cards.or(empty).first()).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should navigate to recipe detail on card click', async ({ authenticatedPage }) => {
    const card = recipeList.recipeCard(recipe().title).first();
    await expect(card).toBeVisible({ timeout: TIMEOUTS.default });

    await card.click();
    await expect(authenticatedPage).toHaveURL(new RegExp(`/recipes/${recipe().identifier}`));
  });

  test('should change sort order', async () => {
    await recipeList.chooseSortOption('name-asc');
    // The custom dropdown exposes the current value via data-current-value
    // on the toggle. Poll for the attribute change rather than reading once.
    await expect(recipeList.sortToggle).toHaveAttribute('data-current-value', 'name-asc');
  });
});
