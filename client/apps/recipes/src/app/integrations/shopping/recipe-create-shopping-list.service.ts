import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { createAsyncState, ERROR_MAPS, ROUTES } from '@yumney/shared/models';
import { CreateShoppingListItem, RecipeDetail, ShoppingApiService } from '../../api';

export interface CreateFromRecipePayload {
  recipe: RecipeDetail;
  desiredServings: number | null;
  ingredients: ReadonlyArray<{ name: string; amount: number | null; unit: string | null }>;
}

@Injectable()
export class RecipeCreateShoppingListService {
  private shoppingApi = inject(ShoppingApiService);
  private router = inject(Router);
  private createState = createAsyncState();

  readonly isCreating = this.createState.isLoading;
  readonly showConfirm = signal(false);
  readonly serverError = signal<string | null>(null);

  open(): void {
    this.serverError.set(null);
    this.showConfirm.set(true);
  }

  cancel(): void {
    if (this.isCreating()) return;
    this.showConfirm.set(false);
  }

  confirm(payload: CreateFromRecipePayload): void {
    const items: CreateShoppingListItem[] = payload.ingredients.map(({ name, amount, unit }) => ({ name, amount, unit }));
    const title =
      payload.recipe.servings !== null && payload.desiredServings !== null
        ? `${payload.recipe.title} (x${payload.desiredServings})`
        : payload.recipe.title;

    this.createState.execute(
      this.shoppingApi.createShoppingList({ title, items, recipeIdentifier: payload.recipe.identifier }),
      ERROR_MAPS.recipes.createShoppingList,
      (created) => {
        this.showConfirm.set(false);
        void this.router.navigateByUrl(ROUTES.shopping.detail(created.identifier));
      },
      (error) => {
        this.showConfirm.set(false);
        this.serverError.set(error);
      },
    );
  }
}
