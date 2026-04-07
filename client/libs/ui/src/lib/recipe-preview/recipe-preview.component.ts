import { Component, ChangeDetectionStrategy, input, output, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { ImportRecipeResponse } from '@yumney/shared/api-client';
import { getGroupedUnits, ensureFormValid, type UnitGroupInfo } from '@yumney/shared/models';
import { EditableListItemComponent } from '../editable-list-item/editable-list-item.component';
import { FormFieldComponent } from '../form-field/form-field.component';
import { SubmitButtonComponent } from '../submit-button/submit-button.component';
import { UnitSelectComponent } from '../unit-select/unit-select.component';
import { RecipeFormController } from './recipe-form';

@Component({
  selector: 'yn-recipe-preview',
  imports: [
    ReactiveFormsModule,
    TranslocoModule,
    EditableListItemComponent,
    FormFieldComponent,
    SubmitButtonComponent,
    UnitSelectComponent,
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
  private controller!: RecipeFormController;

  readonly unitGroups: UnitGroupInfo[] = getGroupedUnits();

  get form() {
    return this.controller.form;
  }
  get ingredients() {
    return this.controller.ingredients;
  }
  get steps() {
    return this.controller.steps;
  }

  ngOnInit(): void {
    this.controller = new RecipeFormController(this.formBuilder, this.recipe());
  }

  addIngredient(): void {
    this.controller.addIngredient();
  }
  removeIngredient(index: number): void {
    this.controller.removeIngredient(index);
  }
  moveIngredientUp(index: number): void {
    this.controller.moveIngredient(index, index - 1);
  }
  moveIngredientDown(index: number): void {
    this.controller.moveIngredient(index, index + 1);
  }

  addStep(): void {
    this.controller.addStep();
  }
  removeStep(index: number): void {
    this.controller.removeStep(index);
  }
  moveStepUp(index: number): void {
    this.controller.moveStep(index, index - 1);
  }
  moveStepDown(index: number): void {
    this.controller.moveStep(index, index + 1);
  }

  onSave(): void {
    if (!ensureFormValid(this.form)) return;
    this.save.emit(this.controller.toResponse());
  }

  onDiscard(): void {
    this.discard.emit();
  }
}
