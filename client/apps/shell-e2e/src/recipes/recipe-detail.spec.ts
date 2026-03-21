import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { RecipeListPage } from '../pages/recipe-list.page';
import { uniqueTitle } from '../helpers/test-data.helper';

test.describe('Recipe Detail (US-031, US-050, US-032, US-033)', () => {
  let recipeIdentifier: string;

  // Create a recipe via manual entry before detail tests
  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();

    // Authenticate
    await page.goto('/auth/login');
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/realms/yumney/**');
    await page.locator('#username').fill('testuser');
    await page.locator('#password').fill('Test1234');
    await page.locator('#kc-login').click();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });

    // Create a manual recipe
    const dashboard = new DashboardPage(page);
    await dashboard.createButton.click();

    const titleInput = page.locator('#title');
    await titleInput.fill(uniqueTitle('E2E Detail Test'));

    // Add an ingredient
    const ingredientName = page.locator('.ingredient-fields input[type="text"]').first();
    await ingredientName.fill('Test Ingredient');

    // Add a step
    const stepDescription = page.locator('.step-fields textarea').first();
    await stepDescription.fill('Test Step');

    const saveButton = page.getByRole('button', { name: /save/i });
    await saveButton.click();

    // Wait for success and extract the identifier from the success or navigation
    await expect(page.locator('.success-banner')).toBeVisible({ timeout: 15_000 });

    // Navigate to recipes to find it
    await page.goto('/recipes');
    await page.waitForTimeout(1000);

    const firstCard = page.locator('.recipe-card').first();
    const href = await firstCard.getAttribute('href');
    recipeIdentifier = href?.replace('/recipes/', '') ?? '';

    await page.close();
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

  test('should show and dismiss delete confirmation dialog (US-033)', async ({ authenticatedPage }) => {
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

    await expect(detail.errorBanner).toBeVisible({ timeout: 10_000 });
  });

  // Delete the recipe last
  test('should delete recipe and navigate to list (US-033)', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await detail.deleteButton.click();
    await expect(detail.confirmDialog).toBeVisible();

    const confirmButton = detail.confirmDialog.locator('.btn-danger');
    await confirmButton.click();

    await expect(authenticatedPage).toHaveURL(/\/recipes$/, { timeout: 10_000 });
  });
});

test.describe('Servings Scaling (US-050)', () => {
  let recipeIdentifier: string;

  test.beforeAll(async ({ browser }) => {
    const page = await browser.newPage();

    await page.goto('/auth/login');
    await page.getByRole('button', { name: /sign in/i }).click();
    await page.waitForURL('**/realms/yumney/**');
    await page.locator('#username').fill('testuser');
    await page.locator('#password').fill('Test1234');
    await page.locator('#kc-login').click();
    await page.waitForURL('**/dashboard', { timeout: 15_000 });

    // Create a recipe with servings and ingredients with amounts
    const dashboard = new DashboardPage(page);
    await dashboard.createButton.click();

    await page.locator('#title').fill(uniqueTitle('E2E Scaling'));
    await page.locator('#servings').fill('4');

    const ingredientName = page.locator('.ingredient-fields input[type="text"]').first();
    await ingredientName.fill('Flour');
    const ingredientAmount = page.locator('.ingredient-fields input[type="number"]').first();
    await ingredientAmount.fill('400');

    await page.getByRole('button', { name: /save/i }).click();
    await expect(page.locator('.success-banner')).toBeVisible({ timeout: 15_000 });

    await page.goto('/recipes');
    await page.waitForTimeout(1000);

    const firstCard = page.locator('.recipe-card').first();
    const href = await firstCard.getAttribute('href');
    recipeIdentifier = href?.replace('/recipes/', '') ?? '';
    await page.close();
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

  // Cleanup
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
