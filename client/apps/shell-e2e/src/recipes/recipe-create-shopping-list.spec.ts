import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';

test.describe('Create shopping list from recipe (US-080)', () => {
  const recipe = setupSharedRecipe(test, 'E2E US080 Recipe', {
    ingredient: 'Spaghetti',
    servings: 4,
  });

  test('opens dialog with scaled preview and auto-name', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.increaseServingsButton.click();
    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('6');

    await detail.shoppingListButton.click();

    const dialog = authenticatedPage.locator('[data-testid="create-shopping-list-dialog"]');
    await expect(dialog).toBeVisible();

    const suggestedTitle = dialog.locator('[data-testid="create-shopping-list-suggested-title"]');
    await expect(suggestedTitle).toHaveText(`${recipe().title} (x6)`);

    const previewItems = dialog.locator('.preview-list li');
    await expect(previewItems).not.toHaveCount(0);
  });

  test('confirms and navigates to the new shopping list', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.shoppingListButton.click();

    const confirmButton = authenticatedPage.locator(
      '[data-testid="create-shopping-list-confirm"]',
    );
    await confirmButton.click();

    await expect(authenticatedPage).toHaveURL(/\/shopping\/lists\/[\w-]+/);
    await expect(authenticatedPage.getByRole('heading', { level: 1 })).toContainText(
      `${recipe().title} (x4)`,
    );
  });
});
