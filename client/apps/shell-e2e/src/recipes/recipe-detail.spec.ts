import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import {
  uniqueTitle,
  openAuthenticatedPage,
  createTestRecipe,
  deleteTestRecipe,
} from '../helpers/test-data.helper';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe Detail (US-031, US-050, US-032, US-033)', () => {
  let recipeIdentifier: string;

  test.beforeAll(async ({ browser }) => {
    const page = await openAuthenticatedPage(browser);
    recipeIdentifier = await createTestRecipe(page, uniqueTitle('E2E Detail Test'));
    await page.context().close();
  });

  test('should display recipe title', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created in beforeAll');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await expect(detail.title).toBeVisible();
    await expect(detail.title).toContainText('E2E Detail Test');
  });

  test('should display ingredients and steps', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await expect(detail.ingredients).not.toHaveCount(0);
    await expect(detail.steps).not.toHaveCount(0);
  });

  test('should have edit, delete, and shopping list buttons', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await expect(detail.editButton).toBeVisible();
    await expect(detail.deleteButton).toBeVisible();
    await expect(detail.shoppingListButton).toBeVisible();
  });

  test('should navigate to edit page (US-032)', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.editButton.click();
    await expect(authenticatedPage).toHaveURL(new RegExp(`/recipes/${recipeIdentifier}/edit`));
  });

  test('should show and dismiss delete confirmation dialog (US-033)', async ({
    authenticatedPage,
  }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();

    const cancelButton = detail.confirmDialog.getByRole('button', { name: /cancel/i });
    await cancelButton.click();
    await expect(detail.confirmDialog).not.toBeVisible();
  });

  test('should show 404 error for non-existent recipe', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await authenticatedPage.goto('/recipes/nonexistent-id-12345');

    await expect(detail.errorBanner).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should delete recipe and navigate to list (US-033)', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();

    const confirmButton = detail.confirmDialog.locator('.btn-danger');
    await confirmButton.click();

    await expect(authenticatedPage).toHaveURL(/\/recipes$/, { timeout: TIMEOUTS.default });
  });
});

test.describe('Servings Scaling (US-050)', () => {
  let recipeIdentifier: string;

  test.beforeAll(async ({ browser }) => {
    const page = await openAuthenticatedPage(browser);
    recipeIdentifier = await createTestRecipe(page, uniqueTitle('E2E Scaling'), {
      ingredient: 'Flour',
      servings: 4,
    });
    await page.context().close();
  });

  test('should display servings controls', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await expect(detail.servingsValue).toHaveText('4');
    await expect(detail.increaseServingsButton).toBeVisible();
    await expect(detail.decreaseServingsButton).toBeVisible();
  });

  test('should increase servings', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('5');
  });

  test('should decrease servings', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.decreaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('3');
  });

  test('should reset to original servings', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.increaseServingsButton.click();
    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('6');

    await detail.resetServingsButton.click();
    await expect(detail.servingsValue).toHaveText('4');
  });

  test.afterAll(async ({ browser }) => {
    if (!recipeIdentifier) return;

    const page = await openAuthenticatedPage(browser);
    await deleteTestRecipe(page, recipeIdentifier);
    await page.context().close();
  });
});
