import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';
import {
  createAsyncState,
  scaleIngredients,
  HttpErrorMap,
  ROUTES,
  VALIDATION,
} from '@yumney/shared/models';
import { BackLinkComponent, ConfirmDialogComponent } from '@yumney/ui';

@Component({
  selector: 'yn-recipe-detail',
  imports: [TranslocoModule, RouterLink, ConfirmDialogComponent, BackLinkComponent],
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
  private loadState = createAsyncState(inject(DestroyRef));
  private deleteState = createAsyncState(inject(DestroyRef));

  recipe = signal<RecipeDetail | null>(null);
  isLoading = this.loadState.isLoading;
  isDeleting = this.deleteState.isLoading;
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

    this.loadState.execute(
      this.recipeApi.getRecipeById(identifier),
      RecipeDetailComponent.detailErrorMap,
      (recipe) => {
        this.recipe.set(recipe);
        this.desiredServings.set(recipe.servings);
      },
      (error) => this.serverError.set(error),
    );
  }

  totalTime = computed(() => {
    const recipe = this.recipe();
    if (!recipe) {
      return null;
    }
    const prep = recipe.prepTimeMinutes ?? 0;
    const cook = recipe.cookTimeMinutes ?? 0;
    return prep === 0 && cook === 0 ? null : prep + cook;
  });

  onIncreaseServings(): void {
    const current = this.desiredServings();
    if (current !== null) {
      this.desiredServings.set(current + 1);
    }
  }

  onDecreaseServings(): void {
    const current = this.desiredServings();
    if (current !== null && current > VALIDATION.RECIPES.SERVINGS.MIN_VALUE) {
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
    this.serverError.set(null);

    this.deleteState.execute(
      this.recipeApi.deleteRecipe(recipe.identifier),
      RecipeDetailComponent.deleteErrorMap,
      () => this.router.navigate([ROUTES.recipes.list]),
      (error) => this.serverError.set(error),
    );
  }

  onDeleteCancelled(): void {
    this.showDeleteConfirm.set(false);
  }
}
