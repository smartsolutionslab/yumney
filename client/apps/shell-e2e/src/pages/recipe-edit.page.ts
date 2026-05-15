import { type Page, type Locator } from '@playwright/test';

/**
 * Edit / manual-create form rendered by <yn-recipe-preview>. Used both
 * from the dashboard "Create recipe" flow and from the recipe-detail
 * Edit link. Selectors mirror the component template; #preview-* IDs
 * are the stable anchors.
 */
export class RecipeEditPage {
  readonly titleInput: Locator;
  readonly servingsInput: Locator;
  readonly descriptionInput: Locator;
  readonly ingredientRows: Locator;
  readonly stepFields: Locator;
  readonly firstIngredientNameInput: Locator;
  readonly firstStepDescriptionInput: Locator;
  readonly saveButton: Locator;

  constructor(private page: Page) {
    this.titleInput = page.locator('#preview-title');
    this.servingsInput = page.locator('#preview-servings');
    this.descriptionInput = page.locator('#preview-description');
    this.ingredientRows = page.locator('.ingredient-row');
    this.stepFields = page.locator('.step-fields');
    // formControlName="name" is unique to ingredient rows.
    this.firstIngredientNameInput = page.locator('input[formControlName="name"]').first();
    // Step description must be scoped to .step-fields — there is also
    // a recipe-level description textarea with the same formControlName.
    this.firstStepDescriptionInput = page.locator('.step-fields textarea[formControlName="description"]').first();
    // Save button lives inside <yn-recipe-preview>; callers should scope
    // via DashboardPage.recipePreview when there could be other .save-btns.
    this.saveButton = page.locator('.save-btn');
  }

  async fillMinimal(title: string, servings = 4): Promise<void> {
    await this.titleInput.fill(title);
    await this.servingsInput.fill(String(servings));
    await this.firstIngredientNameInput.fill('Salt');
    await this.firstStepDescriptionInput.fill('Mix everything.');
  }
}
