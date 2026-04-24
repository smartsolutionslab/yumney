import { test, expect } from '../fixtures/auth.fixture';
import { ShoppingCreatePage } from '../pages/shopping-create.page';
import { ShoppingDetailPage } from '../pages/shopping-detail.page';
import {
  uniqueTitle,
  openAuthenticatedPage,
  createTestRecipe,
  deleteTestRecipe,
} from '../helpers/test-data.helper';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Shopping List — Generate from Recipe (US-040)', () => {
  let recipeIdentifier: string;

  test.beforeAll(async ({ browser }) => {
    const page = await openAuthenticatedPage(browser);
    recipeIdentifier = await createTestRecipe(page, uniqueTitle('E2E Shopping'), {
      ingredient: 'Butter',
    });
    await page.context().close();
  });

  test('should load recipe ingredients on create page', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipeIdentifier);

    await expect(createPage.titleInput).toBeVisible();
    await expect(createPage.ingredientCheckboxes).not.toHaveCount(0);
  });

  test('should have all ingredients selected by default', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipeIdentifier);

    const checkboxes = await createPage.ingredientCheckboxes.all();
    for (const checkbox of checkboxes) {
      await expect(checkbox).toBeChecked();
    }
  });

  test('should deselect and reselect all ingredients', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipeIdentifier);

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

  test('should disable create button when no ingredients selected', async ({
    authenticatedPage,
  }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipeIdentifier);

    await createPage.deselectAllButton.click();
    await expect(createPage.createButton).toBeDisabled();
  });

  test('should create shopping list and navigate to detail', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const createPage = new ShoppingCreatePage(authenticatedPage);
    await createPage.goto(recipeIdentifier);

    await createPage.createButton.click();

    await expect(authenticatedPage).toHaveURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });
    await expect(authenticatedPage.locator('h1')).toBeVisible();
  });

  test('should display shopping lists on overview page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    const cards = authenticatedPage.locator('.list-card');
    const empty = authenticatedPage.locator('.empty-state');
    await expect(cards.or(empty).first()).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should check off an item with strikethrough', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    await authenticatedPage.goto('/shopping');
    const firstCard = authenticatedPage.locator('.list-card').first();
    await firstCard.click();
    await authenticatedPage.waitForURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });

    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.items.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const firstCheckbox = detailPage.itemCheckboxes.first();
    await firstCheckbox.check();

    await expect(detailPage.checkedItems.first()).toBeVisible();
  });

  test('should check all items and reset', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    await authenticatedPage.goto('/shopping');
    const firstCard = authenticatedPage.locator('.list-card').first();
    await firstCard.click();
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
    test.skip(!recipeIdentifier, 'No recipe created');

    await authenticatedPage.goto('/shopping');
    const firstCard = authenticatedPage.locator('.list-card').first();
    await firstCard.click();
    await authenticatedPage.waitForURL(/\/shopping\/.+/, { timeout: TIMEOUTS.default });

    const detailPage = new ShoppingDetailPage(authenticatedPage);
    await expect(detailPage.progress).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(detailPage.progress).toContainText('/');
  });

  test.afterAll(async ({ browser }) => {
    if (!recipeIdentifier) return;

    const page = await openAuthenticatedPage(browser);
    await deleteTestRecipe(page, recipeIdentifier);
    await page.context().close();
  });
});
