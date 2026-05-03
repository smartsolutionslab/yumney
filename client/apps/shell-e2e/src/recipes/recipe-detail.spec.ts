import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe Detail (US-031, US-050, US-032, US-033)', () => {
  const recipe = setupSharedRecipe(test, 'E2E Detail Test');

  test('should display recipe title', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await expect(detail.title).toBeVisible();
    await expect(detail.title).toContainText('E2E Detail Test');
  });

  test('should display ingredients and steps', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await expect(detail.ingredients).not.toHaveCount(0);
    await expect(detail.steps).not.toHaveCount(0);
  });

  test('should have edit, delete, and shopping list buttons', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await expect(detail.editButton).toBeVisible();
    await expect(detail.deleteButton).toBeVisible();
    await expect(detail.shoppingListButton).toBeVisible();
  });

  test('should navigate to edit page (US-032)', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.editButton.click();
    await expect(authenticatedPage).toHaveURL(new RegExp(`/recipes/${recipe().identifier}/edit`));
  });

  test('should show and dismiss delete confirmation dialog (US-033)', async ({
    authenticatedPage,
  }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();

    await detail.confirmCancelButton.click();
    await expect(detail.confirmDialog).not.toBeVisible();
  });

  test('should show 404 error for non-existent recipe', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await authenticatedPage.goto('/recipes/nonexistent-id-12345');

    await expect(detail.errorBanner).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should delete recipe and navigate to list (US-033)', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();

    await detail.confirmDeleteButton.click();

    await expect(authenticatedPage).toHaveURL(/\/recipes$/, { timeout: TIMEOUTS.default });
  });
});

test.describe('Servings Scaling (US-050)', () => {
  const recipe = setupSharedRecipe(test, 'E2E Scaling', {
    ingredient: 'Flour',
    servings: 4,
  });

  test('should display servings controls', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await expect(detail.servingsValue).toHaveText('4');
    await expect(detail.increaseServingsButton).toBeVisible();
    await expect(detail.decreaseServingsButton).toBeVisible();
  });

  test('should increase servings', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('5');
  });

  test('should decrease servings', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.decreaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('3');
  });

  test('should reset to original servings', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.increaseServingsButton.click();
    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('6');

    await detail.resetServingsButton.click();
    await expect(detail.servingsValue).toHaveText('4');
  });
});
