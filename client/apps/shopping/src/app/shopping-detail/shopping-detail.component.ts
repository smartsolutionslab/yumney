import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { ShoppingApiService, ShoppingListDetail, ShoppingListItemResponse } from '../api';
import {
  createAsyncState,
  ERROR_MAPS,
  optimisticSignalUpdate,
  ROUTES,
} from '@yumney/shared/models';
import {
  BackLinkComponent,
  CategorySectionComponent,
  CategoryKey,
  LoadingSpinnerComponent,
} from '@yumney/ui';

const CATEGORY_ORDER: readonly CategoryKey[] = [
  'produce',
  'dairy',
  'meat-fish',
  'bakery',
  'frozen',
  'beverages',
  'pantry',
  'spices',
  'household',
  'other',
];

interface CategoryGroup {
  category: CategoryKey;
  items: ShoppingListItemResponse[];
}

@Component({
  selector: 'yn-shopping-detail',
  imports: [
    TranslocoModule,
    RouterLink,
    BackLinkComponent,
    LoadingSpinnerComponent,
    CategorySectionComponent,
  ],
  templateUrl: './shopping-detail.component.html',
  styleUrl: './shopping-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingDetailComponent implements OnInit {
  protected readonly ROUTES = ROUTES;
  protected readonly availableCategories = CATEGORY_ORDER;

  private shoppingApi = inject(ShoppingApiService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);

  shoppingList = signal<ShoppingListDetail | null>(null);
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;

  checkedCount = computed(() => this.shoppingList()?.items.filter((i) => i.isChecked).length ?? 0);

  totalItemCount = computed(() => this.shoppingList()?.items.length ?? 0);

  groupedItems = computed<CategoryGroup[]>(() => {
    const list = this.shoppingList();
    if (!list) return [];

    const groups = new Map<CategoryKey, ShoppingListItemResponse[]>();
    for (const item of list.items) {
      const key = this.normalize(item.category);
      const bucket = groups.get(key) ?? [];
      bucket.push(item);
      groups.set(key, bucket);
    }

    return CATEGORY_ORDER.filter((category) => groups.has(category)).map((category) => ({
      category,
      items: groups.get(category) ?? [],
    }));
  });

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('shopping.detail.errors.notFound');
      return;
    }

    this.asyncState.execute(
      this.shoppingApi.getShoppingListById(identifier),
      ERROR_MAPS.shopping.detail,
      (list) => this.shoppingList.set(list),
    );
  }

  onToggleItem(item: ShoppingListItemResponse): void {
    const previousState = item.isChecked;
    optimisticSignalUpdate(
      this.shoppingList,
      this.destroyRef,
      () => (item.isChecked = !previousState),
      () => (item.isChecked = previousState),
      (list) => this.shoppingApi.checkOffItem(list.identifier, item.identifier, !previousState),
    );
  }

  onChangeCategory(item: ShoppingListItemResponse, nextCategory: string): void {
    const previousCategory = item.category;
    if (previousCategory === nextCategory) return;
    optimisticSignalUpdate(
      this.shoppingList,
      this.destroyRef,
      () => (item.category = nextCategory),
      () => (item.category = previousCategory),
      (list) => this.shoppingApi.changeItemCategory(list.identifier, item.identifier, nextCategory),
    );
  }

  onCheckAll(): void {
    this.optimisticBatchUpdate(true);
  }

  onReset(): void {
    this.optimisticBatchUpdate(false);
  }

  private normalize(category: string | undefined | null): CategoryKey {
    const value = (category ?? 'other').toLowerCase();
    return (CATEGORY_ORDER as readonly string[]).includes(value) ? (value as CategoryKey) : 'other';
  }

  private optimisticBatchUpdate(checked: boolean): void {
    const list = this.shoppingList();
    if (!list) return;

    const previousStates = list.items.map((i) => i.isChecked);
    optimisticSignalUpdate(
      this.shoppingList,
      this.destroyRef,
      () => {
        for (const item of list.items) item.isChecked = checked;
      },
      () => {
        list.items.forEach((item, i) => (item.isChecked = previousStates[i]));
      },
      (l) => this.shoppingApi.checkOffAllItems(l.identifier, checked),
    );
  }
}
