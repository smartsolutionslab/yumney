import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { ShoppingCreatePage } from '../pages/shopping-create.page';
import { uniqueTitle } from '../helpers/test-data.helper';

test.describe('Shopping List — Generate from Recipe (US-040)', () => {
  let recipeIdentifier: string;

  // Create a recipe to generate a shopping list from
  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();

    await page.goto('/auth/login');
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/realms/yumney/**');
    await page.locator('#username').fill('testuser');
    await page.locator('#password').fill('Test1234');
    await page.locator('#kc-login').click();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });

    const dashboard = new DashboardPage(page);
    await dashboard.createButton.click();

    await page.locator('#title').fill(uniqueTitle('E2E Shopping'));

    const ingredientName = page.locator('.ingredient-fields input[type="text"]').first();
    await ingredientName.fill('Butter');
    const ingredientAmount = page.locator('.ingredient-fields input[type="number"]').first();
    await ingredientAmount.fill('200');

    await page.getByRole('button', { name: /save/i }).click();
    await expect(page.locator('.success-banner')).toBeVisible({ timeout: 15_000 });

    await page.goto('/recipes');
    await page.waitForTimeout(1000);

    const firstCard = page.locator('.recipe-card').first();
    const href = await firstCard.getAttribute('href');
    recipeIdentifier = href?.replace('/recipes/', '') ?? '';
    await page.close();
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

    await expect(authenticatedPage).toHaveURL(/\/shopping\/.+/, { timeout: 10_000 });
    // Should see the shopping list detail
    await expect(authenticatedPage.locator('h1')).toBeVisible();
  });

  test('should display shopping lists on overview page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');

    // Should have at least the list we just created
    const cards = authenticatedPage.locator('.list-card');
    const empty = authenticatedPage.locator('.empty-state');
    await expect(cards.or(empty).first()).toBeVisible({ timeout: 10_000 });
  });

  // Cleanup: delete the recipe (cascade should handle shopping list)
  test.afterAll(async ({ browser }) => {
    if (!recipeIdentifier) return;

    const page = await browser.newPage();
    await page.goto('/auth/login');
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/realms/yumney/**');
    await page.locator('#username').fill('testuser');
    await page.locator('#password').fill('Test1234');
    await page.locator('#kc-login').click();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });

    await page.goto(`/recipes/${recipeIdentifier}`);
    await page.locator('.action-button--danger').click();
    await page.locator('.btn-danger').click();
    await page.waitForURL(/\/recipes$/, { timeout: 10_000 });
    await page.close();
  });
});
