import { Component, ChangeDetectionStrategy, computed, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import {
  mergeRecipeIngredients,
  type MergedIngredient,
  type RecipeForMerge,
  type ScalableIngredient,
} from '@yumney/shared/models';
import { ButtonComponent, DialogShellComponent, LoadingSpinnerComponent } from '@yumney/ui';

export interface MultiRecipeSelection {
  identifier: string;
  title: string;
  originalServings: number | null;
  desiredServings: number;
  ingredients: readonly ScalableIngredient[];
}

@Component({
  selector: 'yn-multi-recipe-preview-dialog',
  imports: [TranslocoModule, ButtonComponent, DialogShellComponent, LoadingSpinnerComponent],
  templateUrl: './multi-recipe-preview-dialog.component.html',
  styleUrl: './multi-recipe-preview-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MultiRecipePreviewDialogComponent {
  recipes = input.required<readonly MultiRecipeSelection[]>();
  isLoading = input(false);
  isCreating = input(false);
  defaultServings = input(4);

  confirmed = output<void>();
  cancelled = output<void>();
  servingsChanged = output<{ identifier: string; servings: number }>();

  mergedIngredients = computed<MergedIngredient[]>(() => {
    if (this.isLoading()) return [];
    const recipes: RecipeForMerge[] = this.recipes().map((recipe) => ({
      ingredients: recipe.ingredients,
      originalServings: recipe.originalServings,
      desiredServings: recipe.desiredServings,
    }));
    return mergeRecipeIngredients(recipes);
  });

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  onIncrease(identifier: string): void {
    const recipe = this.recipes().find((entry) => entry.identifier === identifier);
    if (!recipe) return;
    this.servingsChanged.emit({ identifier, servings: recipe.desiredServings + 1 });
  }

  onDecrease(identifier: string): void {
    const recipe = this.recipes().find((entry) => entry.identifier === identifier);
    if (!recipe || recipe.desiredServings <= 1) return;
    this.servingsChanged.emit({ identifier, servings: recipe.desiredServings - 1 });
  }
}
