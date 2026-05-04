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
import {
  ActivityApiService,
  CreateShoppingListItem,
  RecipeApiService,
  RecipeActivityStats,
  RecipeDetail,
  ShoppingApiService,
} from '../api';
import {
  createAsyncState,
  scaleIngredients,
  ERROR_MAPS,
  ROUTES,
  VALIDATION,
  toggleFavoriteOnItem,
} from '@yumney/shared/models';
import {
  BackLinkComponent,
  ConfirmDialogComponent,
  FavoriteButtonComponent,
  LoadingSpinnerComponent,
} from '@yumney/ui';
import { CreateShoppingListDialogComponent } from './create-shopping-list-dialog/create-shopping-list-dialog.component';

@Component({
  selector: 'yn-recipe-detail',
  imports: [
    TranslocoModule,
    RouterLink,
    ConfirmDialogComponent,
    CreateShoppingListDialogComponent,
    BackLinkComponent,
    LoadingSpinnerComponent,
    FavoriteButtonComponent,
  ],
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private recipeApi = inject(RecipeApiService);
  private shoppingApi = inject(ShoppingApiService);
  private activityApi = inject(ActivityApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private loadState = createAsyncState(this.destroyRef);
  private deleteState = createAsyncState(this.destroyRef);
  private createShoppingListState = createAsyncState(this.destroyRef);

  recipe = signal<RecipeDetail | null>(null);
  recipeStats = signal<RecipeActivityStats | null>(null);
  isLoading = this.loadState.isLoading;
  isDeleting = this.deleteState.isLoading;
  isCreatingShoppingList = this.createShoppingListState.isLoading;
  serverError = signal<string | null>(null);
  desiredServings = signal<number | null>(null);
  showDeleteConfirm = signal(false);
  showCreateShoppingListConfirm = signal(false);

  scaledIngredients = computed(() => {
    const recipe = this.recipe();
    if (!recipe) {
      return [];
    }
    const { ingredients, servings } = recipe;
    const desired = this.desiredServings();
    if (!desired || !servings) {
      return ingredients;
    }
    return scaleIngredients(ingredients, servings, desired);
  });

  isScaled = computed(() => {
    const recipe = this.recipe();
    const desired = this.desiredServings();
    return recipe?.servings != null && desired != null && desired !== recipe.servings;
  });

  lastCookedRelative = computed(() => {
    const stats = this.recipeStats();
    if (!stats?.lastCookedAt) return null;
    const diffMs = Date.now() - new Date(stats.lastCookedAt).getTime();
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    if (diffDays < 1) return { key: 'recipes.detail.stats.lastCookedToday', value: 0 };
    if (diffDays === 1) return { key: 'recipes.detail.stats.lastCookedYesterday', value: 1 };
    if (diffDays < 7) return { key: 'recipes.detail.stats.lastCookedDays', value: diffDays };
    const weeks = Math.floor(diffDays / 7);
    return { key: 'recipes.detail.stats.lastCookedWeeks', value: weeks };
  });

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('recipes.detail.notFound');
      return;
    }

    // Stats can fail silently — the page is still useful without the badge.
    this.activityApi.getRecipeStats(identifier).subscribe({
      next: (stats) => this.recipeStats.set(stats),
      error: () => this.recipeStats.set(null),
    });

    this.loadState.execute(
      this.recipeApi.getRecipeById(identifier),
      ERROR_MAPS.recipes.detail,
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
    const { prepTimeMinutes, cookTimeMinutes } = recipe;
    const prep = prepTimeMinutes ?? 0;
    const cook = cookTimeMinutes ?? 0;
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
      ERROR_MAPS.recipes.delete,
      () => this.router.navigate([ROUTES.recipes.list]),
      (error) => this.serverError.set(error),
    );
  }

  onDeleteCancelled(): void {
    this.showDeleteConfirm.set(false);
  }

  onToggleFavorite(): void {
    toggleFavoriteOnItem(this.recipe, this.destroyRef, (id) => this.recipeApi.toggleFavorite(id));
  }

  onCreateShoppingList(): void {
    if (!this.recipe()) {
      return;
    }
    this.serverError.set(null);
    this.showCreateShoppingListConfirm.set(true);
  }

  onCreateShoppingListCancelled(): void {
    if (this.isCreatingShoppingList()) {
      return;
    }
    this.showCreateShoppingListConfirm.set(false);
  }

  onCreateShoppingListConfirmed(): void {
    const recipe = this.recipe();
    if (!recipe) {
      return;
    }

    const desired = this.desiredServings();
    const items: CreateShoppingListItem[] = this.scaledIngredients().map(
      ({ name, amount, unit }) => ({
        name,
        amount,
        unit,
      }),
    );
    const title =
      recipe.servings !== null && desired !== null ? `${recipe.title} (x${desired})` : recipe.title;

    this.createShoppingListState.execute(
      this.shoppingApi.createShoppingList({
        title,
        items,
        recipeIdentifier: recipe.identifier,
      }),
      ERROR_MAPS.recipes.createShoppingList,
      (created) => {
        this.showCreateShoppingListConfirm.set(false);
        this.router.navigateByUrl(ROUTES.shopping.detail(created.identifier));
      },
      (error) => {
        this.showCreateShoppingListConfirm.set(false);
        this.serverError.set(error);
      },
    );
  }
}
