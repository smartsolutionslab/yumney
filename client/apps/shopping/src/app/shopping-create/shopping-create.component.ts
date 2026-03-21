import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';
import { ShoppingApiService, CreateShoppingListItem } from '@yumney/shared/api-client';
import { mapHttpError, HttpErrorMap } from '@yumney/shared/models';

@Component({
  selector: 'yn-shopping-create',
  imports: [TranslocoModule, FormsModule, RouterLink],
  templateUrl: './shopping-create.component.html',
  styleUrl: './shopping-create.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingCreateComponent implements OnInit {
  private static readonly loadErrorMap: HttpErrorMap = {
    404: 'shopping.create.errors.recipeNotFound',
    default: 'shopping.create.errors.generic',
  };

  private static readonly createErrorMap: HttpErrorMap = {
    default: 'shopping.create.errors.createFailed',
  };

  private recipeApi = inject(RecipeApiService);
  private shoppingApi = inject(ShoppingApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  recipe = signal<RecipeDetail | null>(null);
  isLoading = signal(false);
  isCreating = signal(false);
  serverError = signal<string | null>(null);
  title = signal('');
  ingredientSelections = signal<boolean[]>([]);

  ngOnInit(): void {
    const recipeIdentifier = this.route.snapshot.paramMap.get('recipeIdentifier');
    if (!recipeIdentifier) {
      this.serverError.set('shopping.create.errors.recipeNotFound');
      return;
    }

    this.isLoading.set(true);

    this.recipeApi
      .getRecipeById(recipeIdentifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          this.recipe.set(recipe);
          this.title.set(recipe.title);
          this.ingredientSelections.set(recipe.ingredients.map(() => true));
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, ShoppingCreateComponent.loadErrorMap));
        },
      });
  }

  onToggleIngredient(index: number): void {
    const selections = [...this.ingredientSelections()];
    selections[index] = !selections[index];
    this.ingredientSelections.set(selections);
  }

  onSelectAll(): void {
    this.ingredientSelections.set(this.ingredientSelections().map(() => true));
  }

  onDeselectAll(): void {
    this.ingredientSelections.set(this.ingredientSelections().map(() => false));
  }

  hasSelectedIngredients(): boolean {
    return this.ingredientSelections().some((s) => s);
  }

  onCreateShoppingList(): void {
    const recipe = this.recipe();
    if (!recipe) {
      return;
    }

    const selections = this.ingredientSelections();
    const selectedItems: CreateShoppingListItem[] = recipe.ingredients
      .filter((_, i) => selections[i])
      .map((ingredient) => ({
        name: ingredient.name,
        amount: ingredient.amount,
        unit: ingredient.unit,
      }));

    if (selectedItems.length === 0) {
      return;
    }

    this.isCreating.set(true);
    this.serverError.set(null);

    this.shoppingApi
      .createShoppingList({
        title: this.title(),
        items: selectedItems,
        recipeIdentifier: recipe.identifier,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.isCreating.set(false);
          this.router.navigate(['/shopping', result.identifier]);
        },
        error: (err: HttpErrorResponse) => {
          this.isCreating.set(false);
          this.serverError.set(mapHttpError(err, ShoppingCreateComponent.createErrorMap));
        },
      });
  }
}
