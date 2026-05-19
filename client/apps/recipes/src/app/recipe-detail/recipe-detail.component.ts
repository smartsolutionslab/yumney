import { Component, ChangeDetectionStrategy, inject, OnInit, DestroyRef, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
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
import { RecipeNotesAutosaveService } from './recipe-notes-autosave.service';
import { RecipeScalingService } from './recipe-scaling.service';
import { createAsyncState, type UnitSystem, ERROR_MAPS, ROUTES, UserPreferencesService, toggleFavoriteOnItem } from '@yumney/shared/models';
import {
  BackLinkComponent,
  ButtonComponent,
  ConfirmDialogComponent,
  FavoriteButtonComponent,
  LoadingSpinnerComponent,
  MessageBannerComponent,
  StarRatingComponent,
  UnitToggleComponent,
} from '@yumney/ui';
import { CreateShoppingListDialogComponent } from '../integrations/shopping/create-shopping-list-dialog/create-shopping-list-dialog.component';

@Component({
  selector: 'yn-recipe-detail',
  imports: [
    FormsModule,
    TranslocoModule,
    RouterLink,
    ButtonComponent,
    ConfirmDialogComponent,
    CreateShoppingListDialogComponent,
    BackLinkComponent,
    LoadingSpinnerComponent,
    MessageBannerComponent,
    FavoriteButtonComponent,
    StarRatingComponent,
    UnitToggleComponent,
  ],
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [RecipeNotesAutosaveService, RecipeScalingService],
})
export class RecipeDetailComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private recipeApi = inject(RecipeApiService);
  private shoppingApi = inject(ShoppingApiService);
  private activityApi = inject(ActivityApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private loadState = createAsyncState();
  private deleteState = createAsyncState();
  private createShoppingListState = createAsyncState();
  private notesAutosave = inject(RecipeNotesAutosaveService);
  private scaling = inject(RecipeScalingService);
  private preferences = inject(UserPreferencesService);

  recipe = signal<RecipeDetail | null>(null);
  recipeStats = signal<RecipeActivityStats | null>(null);
  isLoading = this.loadState.isLoading;
  isDeleting = this.deleteState.isLoading;
  isCreatingShoppingList = this.createShoppingListState.isLoading;
  serverError = signal<string | null>(null);

  desiredServings = this.scaling.desiredServings;
  scaledIngredients = this.scaling.scaledIngredients;
  displayedIngredients = this.scaling.displayedIngredients;
  isScaled = this.scaling.isScaled;
  unitSystem = this.scaling.unitSystem;

  showDeleteConfirm = signal(false);
  showCreateShoppingListConfirm = signal(false);

  notesDraft = this.notesAutosave.draft;
  notesSaved = this.notesAutosave.saved;

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

  totalTime = computed(() => {
    const recipe = this.recipe();
    if (!recipe) return null;
    const { prepTimeMinutes, cookTimeMinutes } = recipe;
    const prep = prepTimeMinutes ?? 0;
    const cook = cookTimeMinutes ?? 0;
    return prep === 0 && cook === 0 ? null : prep + cook;
  });

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('recipes.detail.notFound');
      return;
    }

    // Trigger the profile fetch so `preferredUnitSystem` populates. The
    // `unitSystem` computed re-evaluates when the signal lands.
    this.preferences.ensureLoaded();
    this.scaling.attach(this.recipe);

    // Stats can fail silently — the page is still useful without the badge.
    this.activityApi
      .getRecipeStats(identifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stats) => this.recipeStats.set(stats),
        error: () => this.recipeStats.set(null),
      });

    this.loadState.execute(
      this.recipeApi.getRecipeById(identifier),
      ERROR_MAPS.recipes.detail,
      (recipe) => {
        this.recipe.set(recipe);
        this.scaling.initFor(recipe);
        this.notesAutosave.attach(this.recipe);
      },
      (error) => this.serverError.set(error),
    );
  }

  onRatingChange(rating: number): void {
    const recipe = this.recipe();
    if (!recipe) return;
    // Optimistic update so the star fill stays put while the network call runs.
    this.recipe.set({ ...recipe, rating });
    this.recipeApi
      .rateRecipe(recipe.identifier, rating)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => this.recipe.set(recipe),
      });
  }

  onNotesInput(value: string): void {
    this.notesAutosave.update(value);
  }

  onIncreaseServings(): void {
    this.scaling.increase();
  }

  onDecreaseServings(): void {
    this.scaling.decrease();
  }

  onResetServings(): void {
    this.scaling.reset();
  }

  onDelete(): void {
    if (!this.recipe()) return;
    this.showDeleteConfirm.set(true);
  }

  onDeleteConfirmed(): void {
    const recipe = this.recipe();
    if (!recipe) return;

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

  onUnitSystemChange(system: UnitSystem): void {
    this.scaling.setUnitSystem(system);
  }

  onCreateShoppingList(): void {
    if (!this.recipe()) return;
    this.serverError.set(null);
    this.showCreateShoppingListConfirm.set(true);
  }

  onCreateShoppingListCancelled(): void {
    if (this.isCreatingShoppingList()) return;
    this.showCreateShoppingListConfirm.set(false);
  }

  onCreateShoppingListConfirmed(): void {
    const recipe = this.recipe();
    if (!recipe) return;

    const desired = this.desiredServings();
    const items: CreateShoppingListItem[] = this.scaledIngredients().map(({ name, amount, unit }) => ({
      name,
      amount,
      unit,
    }));
    const title = recipe.servings !== null && desired !== null ? `${recipe.title} (x${desired})` : recipe.title;

    this.createShoppingListState.execute(
      this.shoppingApi.createShoppingList({
        title,
        items,
        recipeIdentifier: recipe.identifier,
      }),
      ERROR_MAPS.recipes.createShoppingList,
      (created) => {
        this.showCreateShoppingListConfirm.set(false);
        void this.router.navigateByUrl(ROUTES.shopping.detail(created.identifier));
      },
      (error) => {
        this.showCreateShoppingListConfirm.set(false);
        this.serverError.set(error);
      },
    );
  }
}
