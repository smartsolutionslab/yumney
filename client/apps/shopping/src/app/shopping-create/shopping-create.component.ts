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
import { ShoppingApiService, CreateShoppingListItem } from '@yumney/shared/api-client';
import { createAsyncState, HttpErrorMap } from '@yumney/shared/models';

@Component({
  selector: 'yn-shopping-create',
  imports: [TranslocoModule, RouterLink],
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
  private loadState = createAsyncState(inject(DestroyRef));
  private createState = createAsyncState(inject(DestroyRef));

  recipe = signal<RecipeDetail | null>(null);
  isLoading = this.loadState.isLoading;
  isCreating = this.createState.isLoading;
  serverError = signal<string | null>(null);
  title = signal('');
  ingredientSelections = signal<boolean[]>([]);

  ngOnInit(): void {
    const recipeIdentifier = this.route.snapshot.paramMap.get('recipeIdentifier');
    if (!recipeIdentifier) {
      this.serverError.set('shopping.create.errors.recipeNotFound');
      return;
    }

    this.loadState.execute(
      this.recipeApi.getRecipeById(recipeIdentifier),
      ShoppingCreateComponent.loadErrorMap,
      (recipe) => {
        this.recipe.set(recipe);
        this.title.set(recipe.title);
        this.ingredientSelections.set(recipe.ingredients.map(() => true));
      },
      (error) => this.serverError.set(error),
    );
  }

  onTitleChange(event: Event): void {
    this.title.set((event.target as HTMLInputElement).value);
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

  hasSelectedIngredients = computed(() => this.ingredientSelections().some((s) => s));

  onCreateShoppingList(): void {
    const recipe = this.recipe();
    if (!recipe) {
      return;
    }

    const selections = this.ingredientSelections();
    const selectedItems: CreateShoppingListItem[] = recipe.ingredients
      .filter((_, i) => selections[i])
      .map(({ name, amount, unit }) => ({ name, amount, unit }));

    if (selectedItems.length === 0) {
      return;
    }

    this.serverError.set(null);

    this.createState.execute(
      this.shoppingApi.createShoppingList({
        title: this.title(),
        items: selectedItems,
        recipeIdentifier: recipe.identifier,
      }),
      ShoppingCreateComponent.createErrorMap,
      (result) => this.router.navigate(['/shopping', result.identifier]),
      (error) => this.serverError.set(error),
    );
  }
}
