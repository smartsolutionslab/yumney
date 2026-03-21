import { test, expect } from '../fixtures/auth.fixture';
import { RecipeListPage } from '../pages/recipe-list.page';
import { mockRecipeList } from '../helpers/test-data.helper';

test.describe('Recipe List (US-030, US-034)', () => {
  let recipelist: RecipeListPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    recipelist = new RecipeListPage(authenticatedPage);
  });

  test('should display recipe cards', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) =>
      route.fulfill({ status: 200, json: mockRecipeList }),
    );

    await recipelist.goto();
    await expect(recipelist.recipeCards).toHaveCount(3);
  });

  test('should display recipe titles in cards', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) =>
      route.fulfill({ status: 200, json: mockRecipeList }),
    );

    await recipelist.goto();
    await expect(recipelist.recipeCard('Pasta Carbonara')).toBeVisible();
    await expect(recipelist.recipeCard('Chicken Tikka Masala')).toBeVisible();
    await expect(recipelist.recipeCard('Caesar Salad')).toBeVisible();
  });

  test('should show empty state when no recipes exist', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) =>
      route.fulfill({
        status: 200,
        json: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
      }),
    );

    await recipelist.goto();
    await expect(recipelist.emptyState).toBeVisible();
    await expect(recipelist.ctaButton).toBeVisible();
  });

  test('should filter recipes by search query', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) => {
      const url = new URL(route.request().url());
      const search = url.searchParams.get('search');
      if (search === 'pasta') {
        return route.fulfill({
          status: 200,
          json: {
            items: [mockRecipeList.items[0]],
            page: 1,
            pageSize: 20,
            totalCount: 1,
            totalPages: 1,
          },
        });
      }
      return route.fulfill({ status: 200, json: mockRecipeList });
    });

    await recipelist.goto();
    await expect(recipelist.recipeCards).toHaveCount(3);

    await recipelist.searchInput.fill('pasta');
    await authenticatedPage.waitForTimeout(400); // debounce

    await expect(recipelist.recipeCards).toHaveCount(1);
    await expect(recipelist.recipeCard('Pasta Carbonara')).toBeVisible();
  });

  test('should show no results message for unmatched search', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) => {
      const url = new URL(route.request().url());
      if (url.searchParams.get('search')) {
        return route.fulfill({
          status: 200,
          json: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
        });
      }
      return route.fulfill({ status: 200, json: mockRecipeList });
    });

    await recipelist.goto();
    await recipelist.searchInput.fill('xyznotfound');
    await authenticatedPage.waitForTimeout(400);

    await expect(recipelist.emptyState).toBeVisible();
  });

  test('should clear search and restore results', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) => {
      const url = new URL(route.request().url());
      if (url.searchParams.get('search')) {
        return route.fulfill({
          status: 200,
          json: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
        });
      }
      return route.fulfill({ status: 200, json: mockRecipeList });
    });

    await recipelist.goto();
    await recipelist.searchInput.fill('xyznotfound');
    await authenticatedPage.waitForTimeout(400);
    await expect(recipelist.emptyState).toBeVisible();

    await recipelist.searchClearButton.click();
    await expect(recipelist.recipeCards).toHaveCount(3);
  });

  test('should navigate to recipe detail on card click', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) =>
      route.fulfill({ status: 200, json: mockRecipeList }),
    );

    await recipelist.goto();
    await recipelist.recipeCard('Pasta Carbonara').click();

    await expect(authenticatedPage).toHaveURL(/\/recipes\/recipe-e2e-001/);
  });

  test('should show error on API failure', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes?*', (route) =>
      route.fulfill({ status: 500 }),
    );

    await recipelist.goto();
    await expect(recipelist.errorBanner).toBeVisible();
  });
});
