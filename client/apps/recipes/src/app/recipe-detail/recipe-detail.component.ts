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
import { CreateShoppingListItem, RecipeApiService, RecipeDetail, ShoppingApiService } from '../api';
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
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private loadState = createAsyncState(this.destroyRef);
  private deleteState = createAsyncState(this.destroyRef);
  private createShoppingListState = createAsyncState(this.destroyRef);

  recipe = signal<RecipeDetail | null>(null);
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

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('recipes.detail.notFound');
      return;
    }

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
    const desired = this.desiredServings();
    if (!recipe || desired === null) {
      return;
    }

    const items: CreateShoppingListItem[] = this.scaledIngredients().map(
      ({ name, amount, unit }) => ({
        name,
        amount,
        unit,
      }),
    );

    this.createShoppingListState.execute(
      this.shoppingApi.createShoppingList({
        title: `${recipe.title} (x${desired})`,
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
