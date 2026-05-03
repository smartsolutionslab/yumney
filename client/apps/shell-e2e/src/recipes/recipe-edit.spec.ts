import { test, expect } from '../fixtures/auth.fixture';
import { RecipeDetailPage } from '../pages/recipe-detail.page';
import { RecipeEditPage } from '../pages/recipe-edit.page';
import { RecipeListPage } from '../pages/recipe-list.page';
import { setupSharedRecipe } from '../helpers/shared-recipe';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Recipe edit flow. Previously every test was guarded by
 * `if (hasRecipes)` so a missing seed silently skipped the assertions
 * — a real regression in the edit page wouldn't have caught the test.
 * Plus the title check was `length > 0` which passes for any
 * non-empty string. Both addressed by seeding via setupSharedRecipe
 * and asserting against the known title (#412).
 */
test.describe('Recipe Edit Flow', () => {
  const recipe = setupSharedRecipe(test, 'E2E Edit Test');

  test('navigates from list card to detail page with edit button', async ({
    authenticatedPage,
  }) => {
    const list = new RecipeListPage(authenticatedPage);
    const detail = new RecipeDetailPage(authenticatedPage);
    await list.goto();

    const card = list.recipeCard(recipe().title).first();
    await expect(card).toBeVisible({ timeout: TIMEOUTS.default });
    await card.click();

    await expect(authenticatedPage).toHaveURL(new RegExp(`/recipes/${recipe().identifier}`), {
      timeout: TIMEOUTS.default,
    });

    // Edit is rendered as <a> with routerLink (role=link), not <button>.
    // The pre-#412 test used getByRole('button', /edit/i) and hid the
    // selector mismatch behind an `if (hasRecipes)` skip — exactly the
    // silent-pass pattern this PR was meant to expose.
    await expect(detail.editButton).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('opens edit form pre-populated with the recipe title', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    const editForm = new RecipeEditPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.editButton.click();

    // The edit page reuses <yn-recipe-preview> (same as manual-create),
    // so the title input is #preview-title — the original test's
    // `input[name="title"], #title` selector never existed and was
    // hidden by the `if (hasRecipes)` skip.
    // Tightened from .length > 0 (#412): the form must round-trip the
    // exact title we seeded, not just "any non-empty string".
    await expect(editForm.titleInput).toHaveValue(recipe().title, { timeout: TIMEOUTS.default });
  });

  test('shows ingredient and step fields in the edit form', async ({ authenticatedPage }) => {
    const detail = new RecipeDetailPage(authenticatedPage);
    const editForm = new RecipeEditPage(authenticatedPage);
    await detail.goto(recipe().identifier);

    await detail.editButton.click();

    // recipe-preview renders ingredient rows as .ingredient-row (not
    // .ingredient-fields, which doesn't exist) and step rows as
    // .step-fields. Same selector mismatch as the title input — was
    // hidden by the `if (hasRecipes)` skip in the original test.
    await expect(editForm.ingredientRows.first()).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(editForm.stepFields.first()).toBeVisible({ timeout: TIMEOUTS.default });
  });
});
