import { FormBuilder, FormArray, FormGroup, FormControl, Validators } from '@angular/forms';
import { ImportRecipeResponse, ExtractedIngredient, ExtractedStep } from '@yumney/shared/api-client';
import { VALIDATION } from '@yumney/shared/models';

export interface IngredientFormGroup {
  name: FormControl<string>;
  amount: FormControl<number | null>;
  unit: FormControl<string | null>;
}

export interface StepFormGroup {
  description: FormControl<string>;
}

/**
 * Encapsulates the recipe edit form: building, populating, mutating, and
 * serializing back to the API DTO. Lives outside the component so the
 * component stays focused on view logic.
 */
export class RecipeFormController {
  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly source: ImportRecipeResponse,
  ) {
    this.form = fb.nonNullable.group({
      title: ['', [Validators.required, Validators.maxLength(VALIDATION.RECIPES.RECIPE_TITLE.MAX_LENGTH)]],
      description: [''],
      servings: [null as number | null],
      prepTimeMinutes: [null as number | null],
      cookTimeMinutes: [null as number | null],
      difficulty: [''],
      ingredients: fb.array<FormGroup<IngredientFormGroup>>([]),
      steps: fb.array<FormGroup<StepFormGroup>>([]),
    });

    this.populate(source);
  }

  get ingredients(): FormArray<FormGroup<IngredientFormGroup>> {
    return this.form.controls.ingredients;
  }

  get steps(): FormArray<FormGroup<StepFormGroup>> {
    return this.form.controls.steps;
  }

  addIngredient(): void {
    this.ingredients.push(this.createIngredientGroup({ name: '', amount: null, unit: null }));
  }

  removeIngredient(index: number): void {
    this.ingredients.removeAt(index);
  }

  moveIngredient(from: number, to: number): void {
    this.swapControls(this.ingredients, from, to);
  }

  addStep(): void {
    this.steps.push(this.createStepGroup({ number: this.steps.length + 1, description: '' }));
  }

  removeStep(index: number): void {
    this.steps.removeAt(index);
  }

  moveStep(from: number, to: number): void {
    this.swapControls(this.steps, from, to);
  }

  /** Builds the API response payload from the current form state. */
  toResponse(): ImportRecipeResponse {
    const { title, description, servings, prepTimeMinutes, cookTimeMinutes, difficulty, ingredients, steps } = this.form.getRawValue();
    return {
      title,
      description: description || null,
      servings,
      prepTimeMinutes,
      cookTimeMinutes,
      difficulty: difficulty || null,
      imageUrl: this.source.imageUrl,
      ingredients: ingredients.map(({ name, amount, unit }) => ({ name, amount, unit })),
      steps: steps.map(({ description }, index) => ({ number: index + 1, description })),
    };
  }

  private populate(recipe: ImportRecipeResponse): void {
    this.form.patchValue({
      title: recipe.title,
      description: recipe.description ?? '',
      servings: recipe.servings,
      prepTimeMinutes: recipe.prepTimeMinutes,
      cookTimeMinutes: recipe.cookTimeMinutes,
      difficulty: recipe.difficulty ?? '',
    });

    for (const ingredient of recipe.ingredients) {
      this.ingredients.push(this.createIngredientGroup(ingredient));
    }
    for (const step of recipe.steps) {
      this.steps.push(this.createStepGroup(step));
    }
  }

  private createIngredientGroup(ingredient: ExtractedIngredient): FormGroup<IngredientFormGroup> {
    return this.fb.group({
      name: this.fb.nonNullable.control(ingredient.name, [Validators.required]),
      amount: this.fb.control(ingredient.amount),
      unit: this.fb.control(ingredient.unit),
    }) as FormGroup<IngredientFormGroup>;
  }

  private createStepGroup(step: ExtractedStep): FormGroup<StepFormGroup> {
    return this.fb.group({
      description: this.fb.nonNullable.control(step.description, [Validators.required]),
    }) as FormGroup<StepFormGroup>;
  }

  private swapControls<T extends FormGroup>(array: FormArray<T>, from: number, to: number): void {
    const current = array.at(from);
    const target = array.at(to);
    const currentValue = current.getRawValue();
    const targetValue = target.getRawValue();
    current.patchValue(targetValue);
    target.patchValue(currentValue);
  }
}
