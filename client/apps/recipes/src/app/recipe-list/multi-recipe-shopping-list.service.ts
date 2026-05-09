import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { forkJoin, of } from 'rxjs';
import { createAsyncState, ERROR_MAPS, ROUTES } from '@yumney/shared/models';
import { RecipeApiService, RecipeDetail, ShoppingApiService } from '../api';
import type { MultiRecipeSelection } from './multi-recipe-preview-dialog/multi-recipe-preview-dialog.component';

const FALLBACK_SERVINGS = 4;

@Injectable()
export class MultiRecipeShoppingListService {
  private recipeApi = inject(RecipeApiService);
  private shoppingApi = inject(ShoppingApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private transloco = inject(TranslocoService);
  private destroyRef = inject(DestroyRef);
  private previewLoadState = createAsyncState(this.destroyRef);
  private createState = createAsyncState(this.destroyRef);

  readonly multiSelectMode = signal(false);
  readonly selectedRecipeIds = signal<ReadonlySet<string>>(new Set<string>());
  readonly showPreviewDialog = signal(false);
  readonly previewSelections = signal<readonly MultiRecipeSelection[]>([]);
  readonly isLoadingPreview = this.previewLoadState.isLoading;
  readonly isCreating = this.createState.isLoading;

  readonly selectedCount = computed(() => this.selectedRecipeIds().size);
  readonly hasSelection = computed(() => this.selectedCount() > 0);
  readonly serverError = signal<string | null>(null);

  isSelected(identifier: string): boolean {
    return this.selectedRecipeIds().has(identifier);
  }

  initFromRoute(): void {
    const params = this.route.snapshot.queryParamMap;
    if (params.get('multiSelect') !== 'true') return;

    this.multiSelectMode.set(true);

    const preselect = params.get('preselect');
    if (!preselect) return;

    const ids = preselect
      .split(',')
      .map((id) => id.trim())
      .filter((id) => id.length > 0);
    if (ids.length > 0) {
      this.selectedRecipeIds.set(new Set(ids));
    }
  }

  toggleMode(): void {
    const next = !this.multiSelectMode();
    this.multiSelectMode.set(next);
    if (!next) this.selectedRecipeIds.set(new Set<string>());
  }

  toggleSelection(identifier: string): void {
    const next = new Set(this.selectedRecipeIds());
    if (next.has(identifier)) next.delete(identifier);
    else next.add(identifier);
    this.selectedRecipeIds.set(next);
  }

  clearSelection(): void {
    if (this.selectedRecipeIds().size > 0) {
      this.selectedRecipeIds.set(new Set<string>());
    }
  }

  openPreview(): void {
    if (!this.hasSelection()) return;
    const ids = [...this.selectedRecipeIds()];
    this.serverError.set(null);
    this.showPreviewDialog.set(true);
    this.previewSelections.set([]);

    const fetches = ids.map((identifier) => this.recipeApi.getRecipeById(identifier));
    this.previewLoadState.execute(
      forkJoin(fetches.length > 0 ? fetches : [of(null as unknown as RecipeDetail)]),
      ERROR_MAPS.recipes.createShoppingList,
      (results) => {
        const fallback = this.transloco.translate('recipes.list.multiSelect.preview.fallbackTitle');
        this.previewSelections.set(
          (results as RecipeDetail[]).map((recipe) => ({
            identifier: recipe.identifier,
            title: recipe.title || fallback,
            originalServings: recipe.servings,
            desiredServings: recipe.servings ?? FALLBACK_SERVINGS,
            ingredients: recipe.ingredients,
          })),
        );
      },
      (error) => {
        this.showPreviewDialog.set(false);
        this.serverError.set(error);
      },
    );
  }

  changeServings(identifier: string, servings: number): void {
    this.previewSelections.update((selections) =>
      selections.map((entry) =>
        entry.identifier === identifier ? { ...entry, desiredServings: servings } : entry,
      ),
    );
  }

  cancelPreview(): void {
    if (this.isCreating()) return;
    this.showPreviewDialog.set(false);
    this.previewSelections.set([]);
  }

  confirmPreview(): void {
    const selections = this.previewSelections();
    if (selections.length === 0) return;

    const title = this.transloco.translate('recipes.list.multiSelect.autoTitle', {
      count: selections.length,
    });
    const recipes = selections.map((entry) => ({
      recipeIdentifier: entry.identifier,
      servings: entry.desiredServings,
    }));

    this.serverError.set(null);
    this.createState.execute(
      this.shoppingApi.createShoppingListFromRecipes({ title, recipes }),
      ERROR_MAPS.recipes.createShoppingList,
      (created) => {
        this.showPreviewDialog.set(false);
        this.previewSelections.set([]);
        this.multiSelectMode.set(false);
        this.selectedRecipeIds.set(new Set<string>());
        void this.router.navigateByUrl(ROUTES.shopping.detail(created.identifier));
      },
      (error) => {
        this.showPreviewDialog.set(false);
        this.serverError.set(error);
      },
    );
  }
}
