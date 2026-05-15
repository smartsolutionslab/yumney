import { test, expect } from '../fixtures/auth.fixture';
import { ShoppingCreatePage } from '../pages/shopping-create.page';
import { ShoppingDetailPage } from '../pages/shopping-detail.page';
import { ShoppingListsPage } from '../pages/shopping-lists.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Shopping List — Generate from Recipe (US-040)', () => {
  const recipe = setupSharedRecipe(test, 'E2E Shopping', {
    ingredient: 'Butter',
  });

  test('should load recipe ingredients on create page', async ({ authenticatedPage }) => {
    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipe().identifier);

    await expect(createPage.titleInput).toBeVisible();
    await expect(createPage.ingredientCheckboxes).not.toHaveCount(0);
  });

  test('should have all ingredients selected by default', async ({ authenticatedPage }) => {
    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipe().identifier);

    const checkboxes = await createPage.ingredientCheckboxes.all();
    for (const checkbox of checkboxes) {
      await expect(checkbox).toBeChecked();
    }
  });

  test('should deselect and reselect all ingredients', async ({ authenticatedPage }) => {
    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipe().identifier);

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

  test('should disable create button when no ingredients selected', async ({ authenticatedPage }) => {
    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipe().identifier);

    await createPage.deselectAllButton.click();
    await expect(createPage.createButton).toBeDisabled();
  });

  test('should create shopping list and navigate to detail', async ({ authenticatedPage }) => {
    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipe().identifier);

    await createPage.createButton.click();

    await expect(authenticatedPage).toHaveURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });
    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.heading).toBeVisible();
  });

  test('should display shopping lists on overview page', async ({ authenticatedPage }) => {
    const lists = new ShoppingListsPage(authenticatedPage);
    await lists.goto();

    await expect(lists.listCards.or(lists.emptyState).first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  // The next three tests rely on the list created in the previous test
  // being visible at /shopping/lists. The original symptom traced back to
  // the ShoppingList projection's out-of-order delivery race that #621
  // closed via INSERT … ON CONFLICT DO UPDATE in ShoppingListProjection;
  // un-fixme'd here to let CI confirm the fix held.
  test('should check off an item with strikethrough', async ({ authenticatedPage }) => {
    const lists = new ShoppingListsPage(authenticatedPage);
    await lists.goto();
    await lists.listCards.first().click();
    await authenticatedPage.waitForURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });

    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.items.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const firstCheckbox = detailPage.itemCheckboxes.first();
    await firstCheckbox.check();

    await expect(detailPage.checkedItems.first()).toBeVisible();
  });

  test('should check all items and reset', async ({ authenticatedPage }) => {
    const lists = new ShoppingListsPage(authenticatedPage);
    await lists.goto();
    await lists.listCards.first().click();
    await authenticatedPage.waitForURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });

    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.items.first()).toBeVisible({ timeout: TIMEOUTS.default });

    await detailPage.checkAllButton.click();
    const allItems = await detailPage.items.all();
    for (const item of allItems) {
      await expect(item).toHaveClass(/checked/);
    }

    await detailPage.resetButton.click();
    for (const item of allItems) {
      await expect(item).not.toHaveClass(/checked/);
    }
  });

  test('should show progress counter', async ({ authenticatedPage }) => {
    const lists = new ShoppingListsPage(authenticatedPage);
    await lists.goto();
    await lists.listCards.first().click();
    await authenticatedPage.waitForURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });

    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.progress).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(detailPage.progress).toContainText('/');
  });
});
