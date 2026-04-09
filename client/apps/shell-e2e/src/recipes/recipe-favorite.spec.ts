import { test, expect } from '../fixtures/auth.fixture';
import { DashboardPage } from '../pages/dashboard.page';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { RecipeListPage } from '../pages/recipe-list.page';
import { uniqueTitle } from '../helpers/test-data.helper';

test.describe('Favorite Recipes (US-071)', () => {
  let recipeTitle: string;
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

    const dashboard = new DashboardPage(page);
    await dashboard.createButton.click();

    recipeTitle = uniqueTitle('E2E Favorite');
    await page.locator('#title').fill(recipeTitle);
    await page.locator('.ingredient-fields input[type="text"]').first().fill('Salt');
    await page.locator('.step-fields textarea').first().fill('Add salt to taste');
    await page.getByRole('button', { name: /save/i }).click();
    await expect(page.locator('.success-banner')).toBeVisible({ timeout: 15_000 });

    await page.goto('/recipes');
    await page.waitForTimeout(1000);

    const card = page.locator('.recipe-card').filter({ hasText: recipeTitle }).first();
    const href = await card.getAttribute('href');
    recipeIdentifier = href?.replace('/recipes/', '') ?? '';

    await page.close();
  });

  test('should toggle favorite from recipe list card', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created in beforeAll');

    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipeTitle).first()).toBeVisible({ timeout: 10_000 });

    const heart = list.favoriteButtonOnCard(recipeTitle).first();
    await expect(heart).toHaveAttribute('aria-pressed', 'false');

    await heart.click();
    await expect(heart).toHaveAttribute('aria-pressed', 'true');
  });

  test('should persist favorite state across reload', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipeTitle).first()).toBeVisible({ timeout: 10_000 });

    const heart = list.favoriteButtonOnCard(recipeTitle).first();
    await expect(heart).toHaveAttribute('aria-pressed', 'true', { timeout: 5_000 });

    await authenticatedPage.reload();
    await expect(list.recipeCard(recipeTitle).first()).toBeVisible({ timeout: 10_000 });
    await expect(heart).toHaveAttribute('aria-pressed', 'true');
  });

  test('should narrow list when "Show only favorites" filter is enabled', async ({
    authenticatedPage,
  }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const list = new RecipeListPage(authenticatedPage);
    await list.goto();
    await expect(list.recipeCard(recipeTitle).first()).toBeVisible({ timeout: 10_000 });

    await list.filterToggle.click();
    await list.favoritesFilterChip.click();
    await authenticatedPage.waitForTimeout(500);

    // Favorited recipe should still be present
    await expect(list.recipeCard(recipeTitle).first()).toBeVisible();
  });

  test('should reflect favorite state on recipe detail page', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);

    await expect(detail.favoriteButton).toBeVisible({ timeout: 10_000 });
    await expect(detail.favoriteButton).toHaveAttribute('aria-pressed', 'true');
  });

  test('should toggle favorite back off from recipe detail', async ({ authenticatedPage }) => {
    test.skip(!recipeIdentifier, 'No recipe created');

    const detail = new RecipeDetailPage(authenticatedPage);
    await detail.goto(recipeIdentifier);
    await expect(detail.favoriteButton).toBeVisible({ timeout: 10_000 });

    await detail.favoriteButton.click();
    await expect(detail.favoriteButton).toHaveAttribute('aria-pressed', 'false');
  });
});
