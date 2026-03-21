import { test, expect } from '../fixtures/auth.fixture';
import {
  mockRecipeDetail,
  mockShoppingListDetail,
  mockShoppingLists,
} from '../helpers/test-data.helper';
import { ShoppingCreatePage } from '../pages/shopping-create.page';

test.describe('Shopping List — Generate from Recipe (US-040)', () => {
  let createPage: ShoppingCreatePage;

  test.beforeEach(async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/recipe-e2e-001', (route) =>
      route.fulfill({ status: 200, json: mockRecipeDetail }),
    );

    createPage = new ShoppingCreatePage(authenticatedPage);
  });

  test('should load recipe ingredients with all selected', async ({ authenticatedPage }) => {
    await createPage.goto('recipe-e2e-001');

    await expect(createPage.titleInput).toHaveValue('Pasta Carbonara');
    await expect(createPage.ingredientCheckboxes).toHaveCount(5);

    const checkboxes = await createPage.ingredientCheckboxes.all();
    for (const checkbox of checkboxes) {
      await expect(checkbox).toBeChecked();
    }
  });

  test('should deselect and reselect ingredients', async ({ authenticatedPage }) => {
    await createPage.goto('recipe-e2e-001');

    await createPage.deselectAllButton.click();

    const checkboxes = await createPage.ingredientCheckboxes.all();
    for (const checkbox of checkboxes) {
      await expect(checkbox).not.toBeChecked();
    }

    await createPage.selectAllButton.click();

    for (const checkbox of checkboxes) {
      await expect(checkbox).toBeChecked();
    }
  });

  test('should toggle individual ingredient', async ({ authenticatedPage }) => {
    await createPage.goto('recipe-e2e-001');

    const secondCheckbox = createPage.ingredientCheckboxes.nth(1);
    await secondCheckbox.uncheck();
    await expect(secondCheckbox).not.toBeChecked();

    await secondCheckbox.check();
    await expect(secondCheckbox).toBeChecked();
  });

  test('should create shopping list and navigate to detail', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/shopping-lists', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 201, json: mockShoppingListDetail });
      }
      return route.continue();
    });

    await createPage.goto('recipe-e2e-001');
    await createPage.createButton.click();

    await expect(authenticatedPage).toHaveURL(/\/shopping\/list-e2e-001/);
  });

  test('should not create when no ingredients selected', async ({ authenticatedPage }) => {
    await createPage.goto('recipe-e2e-001');

    await createPage.deselectAllButton.click();
    await expect(createPage.createButton).toBeDisabled();
  });

  test('should show error on create failure', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/shopping-lists', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({ status: 500, json: { detail: 'Server error' } });
      }
      return route.continue();
    });

    await createPage.goto('recipe-e2e-001');
    await createPage.createButton.click();

    await expect(createPage.errorBanner).toBeVisible();
  });
});

test.describe('Shopping Lists Overview', () => {
  test('should display shopping lists', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/shopping-lists', (route) =>
      route.fulfill({ status: 200, json: mockShoppingLists }),
    );

    await authenticatedPage.goto('/shopping');

    const cards = authenticatedPage.locator('.list-card');
    await expect(cards).toHaveCount(2);
    await expect(cards.first()).toContainText('Pasta Carbonara');
  });

  test('should show empty state when no lists', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/shopping-lists', (route) =>
      route.fulfill({ status: 200, json: [] }),
    );

    await authenticatedPage.goto('/shopping');

    await expect(authenticatedPage.locator('.empty-state')).toBeVisible();
  });
});
