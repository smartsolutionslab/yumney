import { test, expect } from '../fixtures/auth.fixture';
import { CreateShoppingListDialogPage } from '../pages/create-shopping-list-dialog.page';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { ShoppingDetailPage } from '../pages/shopping-detail.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';

test.describe('Create shopping list from recipe (US-080)', () => {
  const recipe = setupSharedRecipe(test, 'E2E US080 Recipe', {
    ingredient: 'Spaghetti',
    servings: 4,
  });

  test('opens dialog with scaled preview and auto-name', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    const dialog = new CreateShoppingListDialogPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.increaseServingsButton.click();
    await detail.increaseServingsButton.click();
    await expect(detail.servingsValue).toHaveText('6');

    await detail.shoppingListButton.click();

    await expect(dialog.root).toBeVisible();
    await expect(dialog.suggestedTitle).toHaveText(`${recipe().title} (x6)`);
    await expect(dialog.previewItems).not.toHaveCount(0);
  });

  test('confirms and navigates to the new shopping list', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    const dialog = new CreateShoppingListDialogPage(authenticatedPage);
    const shoppingDetail = new ShoppingDetailPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.shoppingListButton.click();

    await dialog.confirmButton.click();

    await expect(authenticatedPage).toHaveURL(/\/shopping\/lists\/[\w-]+/);
    await expect(shoppingDetail.heading).toContainText(`${recipe().title} (x4)`);
  });
});
