import { Component, ChangeDetectionStrategy, input, output, OnInit, inject } from '@angular/core';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormArray,
  FormGroup,
  FormControl,
  Validators,
} from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import {
  ImportRecipeResponse,
  ExtractedIngredient,
  ExtractedStep,
} from '@yumney/shared/api-client';
import { VALIDATION, getGroupedUnits, type UnitGroupInfo } from '@yumney/shared/models';
import { EditableListItemComponent } from '../editable-list-item/editable-list-item.component';
import { FormFieldComponent } from '../form-field/form-field.component';
import { SubmitButtonComponent } from '../submit-button/submit-button.component';

interface IngredientFormGroup {
  name: FormControl<string>;
  amount: FormControl<number | null>;
  unit: FormControl<string | null>;
}

interface StepFormGroup {
  description: FormControl<string>;
}

@Component({
  selector: 'yn-recipe-preview',
  imports: [
    ReactiveFormsModule,
    TranslocoModule,
    EditableListItemComponent,
    FormFieldComponent,
    SubmitButtonComponent,
  ],
  templateUrl: './recipe-preview.component.html',
  styleUrl: './recipe-preview.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipePreviewComponent implements OnInit {
  recipe = input.required<ImportRecipeResponse>();
  isSaving = input(false);
  previewTitle = input<string | undefined>();

  save = output<ImportRecipeResponse>();
  discard = output<void>();

  private formBuilder = inject(FormBuilder);

  readonly unitGroups: UnitGroupInfo[] = getGroupedUnits();

  form = this.formBuilder.nonNullable.group({
    title: [
      '',
      [Validators.required, Validators.maxLength(VALIDATION.RECIPES.RECIPE_TITLE.MAX_LENGTH)],
    ],
    description: [''],
    servings: [null as number | null],
    prepTimeMinutes: [null as number | null],
    cookTimeMinutes: [null as number | null],
    difficulty: [''],
    ingredients: this.formBuilder.array<FormGroup<IngredientFormGroup>>([]),
    steps: this.formBuilder.array<FormGroup<StepFormGroup>>([]),
  });

  get ingredients(): FormArray<FormGroup<IngredientFormGroup>> {
    return this.form.controls.ingredients;
  }

  get steps(): FormArray<FormGroup<StepFormGroup>> {
    return this.form.controls.steps;
  }

  ngOnInit(): void {
    const recipe = this.recipe();
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

  addIngredient(): void {
    this.ingredients.push(this.createIngredientGroup({ name: '', amount: null, unit: null }));
  }

  removeIngredient(index: number): void {
    this.ingredients.removeAt(index);
  }

  moveIngredientUp(index: number): void {
    this.swapControls(this.ingredients, index, index - 1);
  }

  moveIngredientDown(index: number): void {
    this.swapControls(this.ingredients, index, index + 1);
  }

  addStep(): void {
    this.steps.push(this.createStepGroup({ number: this.steps.length + 1, description: '' }));
  }

  removeStep(index: number): void {
    this.steps.removeAt(index);
  }

  moveStepUp(index: number): void {
    this.swapControls(this.steps, index, index - 1);
  }

  moveStepDown(index: number): void {
    this.swapControls(this.steps, index, index + 1);
  }

  onSave(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const {
      title,
      description,
      servings,
      prepTimeMinutes,
      cookTimeMinutes,
      difficulty,
      ingredients,
      steps,
    } = this.form.getRawValue();
    const result: ImportRecipeResponse = {
      title,
      description: description || null,
      servings,
      prepTimeMinutes,
      cookTimeMinutes,
      difficulty: difficulty || null,
      imageUrl: this.recipe().imageUrl,
      ingredients: ingredients.map(({ name, amount, unit }) => ({ name, amount, unit })),
      steps: steps.map(({ description }, index) => ({ number: index + 1, description })),
    };

    this.save.emit(result);
  }

  onDiscard(): void {
    this.discard.emit();
  }

  private createIngredientGroup(ingredient: ExtractedIngredient): FormGroup<IngredientFormGroup> {
    return this.formBuilder.group({
      name: this.formBuilder.nonNullable.control(ingredient.name, [Validators.required]),
      amount: this.formBuilder.control(ingredient.amount),
      unit: this.formBuilder.control(ingredient.unit),
    }) as FormGroup<IngredientFormGroup>;
  }

  private createStepGroup(step: ExtractedStep): FormGroup<StepFormGroup> {
    return this.formBuilder.group({
      description: this.formBuilder.nonNullable.control(step.description, [Validators.required]),
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
