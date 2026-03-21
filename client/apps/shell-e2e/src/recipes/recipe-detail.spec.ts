import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { mockRecipeDetail } from '../helpers/test-data.helper';

test.describe('Recipe Detail (US-031, US-050, US-033)', () => {
  let detail: RecipeDetailPage;

  test.beforeEach(async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/recipe-e2e-001', (route) => {
      if (route.request().method() === 'GET') {
        return route.fulfill({ status: 200, json: mockRecipeDetail });
      }
      return route.continue();
    });

    detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto('recipe-e2e-001');
  });

  test('should display recipe title and description', async () => {
    await expect(detail.title).toHaveText('Pasta Carbonara');
    await expect(detail.description).toContainText('classic Italian pasta dish');
  });

  test('should display all ingredients', async () => {
    await expect(detail.ingredients).toHaveCount(5);
  });

  test('should display all steps in order', async () => {
    await expect(detail.steps).toHaveCount(4);
    await expect(detail.steps.first()).toContainText('Cook spaghetti');
    await expect(detail.steps.last()).toContainText('Combine pasta');
  });

  test('should display initial servings', async () => {
    await expect(detail.servingsValue).toHaveText('4');
  });

  test('should increase servings and scale ingredients (US-050)', async () => {
    await detail.increaseServingsButton.click();
    await detail.increaseServingsButton.click();

    await expect(detail.servingsValue).toHaveText('6');
    // Spaghetti: 400g * 6/4 = 600g
    await expect(detail.ingredients.first()).toContainText('600');
  });

  test('should decrease servings and scale ingredients (US-050)', async () => {
    await detail.decreaseServingsButton.click();
    await detail.decreaseServingsButton.click();

    await expect(detail.servingsValue).toHaveText('2');
    // Spaghetti: 400g * 2/4 = 200g
    await expect(detail.ingredients.first()).toContainText('200');
  });

  test('should not decrease below 1 serving (US-050)', async () => {
    // Set to 1
    for (let i = 0; i < 5; i++) {
      await detail.decreaseServingsButton.click();
    }
    await expect(detail.servingsValue).toHaveText('1');
    await expect(detail.decreaseServingsButton).toBeDisabled();
  });

  test('should reset servings to original (US-050)', async () => {
    await detail.increaseServingsButton.click();
    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('6');

    await detail.resetServingsButton.click();
    await expect(detail.servingsValue).toHaveText('4');
  });

  test('should display source URL link', async () => {
    await expect(detail.sourceLink).toBeVisible();
    await expect(detail.sourceLink).toHaveAttribute('href', 'https://example.com/carbonara');
  });

  test('should have edit, delete, and shopping list action buttons', async () => {
    await expect(detail.editButton).toBeVisible();
    await expect(detail.deleteButton).toBeVisible();
    await expect(detail.shoppingListButton).toBeVisible();
  });

  test('should navigate to edit page on edit click', async ({ authenticatedPage }) => {
    await detail.editButton.click();
    await expect(authenticatedPage).toHaveURL(/\/recipes\/recipe-e2e-001\/edit/);
  });

  test('should show confirmation dialog on delete click (US-033)', async () => {
    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();
  });

  test('should dismiss confirmation dialog on cancel (US-033)', async () => {
    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();

    const cancelButton = detail.confirmDialog.getByRole('button', { name: /cancel/i });
    await cancelButton.click();

    await expect(detail.confirmDialog).not.toBeVisible();
  });

  test('should delete recipe and navigate to list (US-033)', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/recipe-e2e-001', (route) => {
      if (route.request().method() === 'DELETE') {
        return route.fulfill({ status: 204 });
      }
      return route.fulfill({ status: 200, json: mockRecipeDetail });
    });

    await detail.deleteButton.click();
    const confirmButton = detail.confirmDialog.getByRole('button').nth(1); // danger button
    await confirmButton.click();

    await expect(authenticatedPage).toHaveURL(/\/recipes$/);
  });

  test('should show 404 error for non-existent recipe', async ({ authenticatedPage }) => {
    await authenticatedPage.route('**/api/v1/recipes/nonexistent', (route) =>
      route.fulfill({ status: 404 }),
    );

    await authenticatedPage.goto('/recipes/nonexistent');
    await expect(detail.errorBanner).toBeVisible();
  });
});
