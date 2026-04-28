import { test, expect } from '../fixtures/auth.fixture';
import { SELECTORS } from '../helpers/selectors';
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

  test('navigates from list card to detail page with edit button', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/recipes');

    const card = authenticatedPage.locator(SELECTORS.recipe.card, { hasText: recipe().title }).first();
    await expect(card).toBeVisible({ timeout: TIMEOUTS.default });
    await card.click();

    await expect(authenticatedPage).toHaveURL(
      new RegExp(`/recipes/${recipe().identifier}`),
      { timeout: TIMEOUTS.default },
    );

    // Edit is rendered as <a> with routerLink (role=link), not <button>.
    // The pre-#412 test used getByRole('button', /edit/i) and hid the
    // selector mismatch behind an `if (hasRecipes)` skip — exactly the
    // silent-pass pattern this PR was meant to expose.
    const editBtn = authenticatedPage.getByRole('link', { name: /edit/i });
    await expect(editBtn).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('opens edit form pre-populated with the recipe title', async ({ authenticatedPage }) => {
    await authenticatedPage.goto(`/recipes/${recipe().identifier}`);

    // Edit is rendered as <a> with routerLink (role=link), not <button>.
    // The pre-#412 test used getByRole('button', /edit/i) and hid the
    // selector mismatch behind an `if (hasRecipes)` skip — exactly the
    // silent-pass pattern this PR was meant to expose.
    const editBtn = authenticatedPage.getByRole('link', { name: /edit/i });
    await editBtn.click();

    const titleInput = authenticatedPage.locator('input[name="title"], #title');
    // Tightened from .length > 0 (#412): the form must round-trip the
    // exact title we seeded, not just "any non-empty string".
    await expect(titleInput).toHaveValue(recipe().title, { timeout: TIMEOUTS.default });
  });

  test('shows ingredient and step fields in the edit form', async ({ authenticatedPage }) => {
    await authenticatedPage.goto(`/recipes/${recipe().identifier}`);

    // Edit is rendered as <a> with routerLink (role=link), not <button>.
    // The pre-#412 test used getByRole('button', /edit/i) and hid the
    // selector mismatch behind an `if (hasRecipes)` skip — exactly the
    // silent-pass pattern this PR was meant to expose.
    const editBtn = authenticatedPage.getByRole('link', { name: /edit/i });
    await editBtn.click();

    await expect(authenticatedPage.locator(SELECTORS.form.ingredients)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await expect(authenticatedPage.locator(SELECTORS.form.steps)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });
});
