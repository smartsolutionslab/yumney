import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeDetail } from '../api';
import { ShoppingApiService, CreateShoppingListItem } from '../api';
import { createAsyncState, ERROR_MAPS, ROUTES, VALIDATION } from '@yumney/shared/models';
import { BackLinkComponent, LoadingSpinnerComponent, MessageBannerComponent } from '@yumney/ui';

@Component({
  selector: 'yn-shopping-create',
  imports: [TranslocoModule, BackLinkComponent, LoadingSpinnerComponent, MessageBannerComponent],
  templateUrl: './shopping-create.component.html',
  styleUrl: './shopping-create.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingCreateComponent implements OnInit {
  protected readonly VALIDATION = VALIDATION;

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
      ERROR_MAPS.shopping.createLoad,
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
      ERROR_MAPS.shopping.create,
      (result) => this.router.navigateByUrl(ROUTES.shopping.detail(result.identifier)),
      (error) => this.serverError.set(error),
    );
  }
}
