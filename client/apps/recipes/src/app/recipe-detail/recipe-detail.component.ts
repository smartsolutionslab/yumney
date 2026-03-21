import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';
import { mapHttpError, HttpErrorMap, scaleIngredients } from '@yumney/shared/models';
import { ConfirmDialogComponent } from '@yumney/ui';

@Component({
  selector: 'yn-recipe-detail',
  imports: [TranslocoModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailComponent implements OnInit {
  private static readonly detailErrorMap: HttpErrorMap = {
    404: 'recipes.detail.notFound',
    default: 'recipes.detail.errors.generic',
  };

  private static readonly deleteErrorMap: HttpErrorMap = {
    404: 'recipes.detail.delete.errors.notFound',
    default: 'recipes.detail.delete.errors.generic',
  };

  private recipeApi = inject(RecipeApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  recipe = signal<RecipeDetail | null>(null);
  isLoading = signal(false);
  isDeleting = signal(false);
  serverError = signal<string | null>(null);
  desiredServings = signal<number | null>(null);
  showDeleteConfirm = signal(false);

  scaledIngredients = computed(() => {
    const recipe = this.recipe();
    if (!recipe) {
      return [];
    }
    const desired = this.desiredServings();
    if (!desired || !recipe.servings) {
      return recipe.ingredients;
    }
    return scaleIngredients(recipe.ingredients, recipe.servings, desired);
  });

  isScaled = computed(() => {
    const recipe = this.recipe();
    const desired = this.desiredServings();
    return recipe?.servings != null && desired != null && desired !== recipe.servings;
  });

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('recipes.detail.notFound');
      return;
    }

    this.isLoading.set(true);

    this.recipeApi
      .getRecipeById(identifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          this.recipe.set(recipe);
          this.desiredServings.set(recipe.servings);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, RecipeDetailComponent.detailErrorMap));
        },
      });
  }

  totalTime(): number | null {
    const recipe = this.recipe();
    if (!recipe) {
      return null;
    }
    const { prepTimeMinutes, cookTimeMinutes } = recipe;
    const prep = prepTimeMinutes ?? 0;
    const cook = cookTimeMinutes ?? 0;
    return prep === 0 && cook === 0 ? null : prep + cook;
  }

  onIncreaseServings(): void {
    const current = this.desiredServings();
    if (current !== null) {
      this.desiredServings.set(current + 1);
    }
  }

  onDecreaseServings(): void {
    const current = this.desiredServings();
    if (current !== null && current > 1) {
      this.desiredServings.set(current - 1);
    }
  }

  onResetServings(): void {
    const recipe = this.recipe();
    if (recipe?.servings != null) {
      this.desiredServings.set(recipe.servings);
    }
  }

  onDelete(): void {
    if (!this.recipe()) {
      return;
    }
    this.showDeleteConfirm.set(true);
  }

  onDeleteConfirmed(): void {
    const recipe = this.recipe();
    if (!recipe) {
      return;
    }

    this.showDeleteConfirm.set(false);
    this.isDeleting.set(true);
    this.serverError.set(null);

    this.recipeApi
      .deleteRecipe(recipe.identifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isDeleting.set(false);
          this.router.navigate(['/recipes']);
        },
        error: (err: HttpErrorResponse) => {
          this.isDeleting.set(false);
          this.serverError.set(mapHttpError(err, RecipeDetailComponent.deleteErrorMap));
        },
      });
  }

  onDeleteCancelled(): void {
    this.showDeleteConfirm.set(false);
  }
}
