import { test, expect } from '../fixtures/auth.fixture';
import { SELECTORS } from '../helpers/selectors';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe Edit Flow', () => {
  test('should navigate to recipe detail and show edit button', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/recipes');

    const firstCard = authenticatedPage.locator(SELECTORS.recipe.card).first();
    const hasRecipes = (await firstCard.count()) > 0;

    if (hasRecipes) {
      await firstCard.click();
      await expect(authenticatedPage).toHaveURL(/\/recipes\//, { timeout: TIMEOUTS.default });

      const editBtn = authenticatedPage.getByRole('button', { name: /edit/i });
      await expect(editBtn).toBeVisible({ timeout: TIMEOUTS.default });
    }
  });

  test('should open edit form with pre-populated data', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/recipes');

    const firstCard = authenticatedPage.locator(SELECTORS.recipe.card).first();
    const hasRecipes = (await firstCard.count()) > 0;

    if (hasRecipes) {
      await firstCard.click();
      await expect(authenticatedPage).toHaveURL(/\/recipes\//, { timeout: TIMEOUTS.default });

      const editBtn = authenticatedPage.getByRole('button', { name: /edit/i });
      await editBtn.click();

      // Edit form should have a title input with existing value
      const titleInput = authenticatedPage.locator('input[name="title"], #title');
      await expect(titleInput).toBeVisible({ timeout: TIMEOUTS.default });
      const titleValue = await titleInput.inputValue();
      expect(titleValue.length).toBeGreaterThan(0);
    }
  });

  test('should show ingredient and step fields in edit form', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/recipes');

    const firstCard = authenticatedPage.locator(SELECTORS.recipe.card).first();
    const hasRecipes = (await firstCard.count()) > 0;

    if (hasRecipes) {
      await firstCard.click();
      await authenticatedPage.waitForURL(/\/recipes\//);

      const editBtn = authenticatedPage.getByRole('button', { name: /edit/i });
      await editBtn.click();

      await expect(authenticatedPage.locator(SELECTORS.form.ingredients)).toBeVisible({
        timeout: TIMEOUTS.default,
      });
      await expect(authenticatedPage.locator(SELECTORS.form.steps)).toBeVisible({
        timeout: TIMEOUTS.default,
      });
    }
  });
});
