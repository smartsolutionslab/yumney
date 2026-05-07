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
  BRING_FALLBACK_URL,
  buildBringImportUrl,
  createAsyncState,
  ERROR_MAPS,
  groupByCategory,
  optimisticSignalUpdate,
  ROUTES,
  ToastService,
  type CategoryGroup,
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

// How long to give the OS to launch the Bring! app before assuming it isn't
// installed and falling back to the marketing site. 1.5 s is the de-facto
// industry value for app-link probes — short enough to feel responsive,
// long enough that a slow handoff doesn't false-positive.
const BRING_LAUNCH_PROBE_MS = 1500;

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
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);

  shoppingList = signal<ShoppingListDetail | null>(null);
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;

  checkedCount = computed(() => this.shoppingList()?.items.filter((i) => i.isChecked).length ?? 0);

  totalItemCount = computed(() => this.shoppingList()?.items.length ?? 0);

  groupedItems = computed<CategoryGroup<ShoppingListItemResponse, CategoryKey>[]>(() => {
    const list = this.shoppingList();
    if (!list) return [];
    return groupByCategory(list.items, (item) => this.normalize(item.category), {
      order: CATEGORY_ORDER,
    });
  });

  // Items still to buy — already-checked items have been added to the cart, so
  // sending them to Bring! would re-add things the user has already grabbed.
  uncheckedItems = computed(() => this.shoppingList()?.items.filter((i) => !i.isChecked) ?? []);

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

  onSendToBring(): void {
    const items = this.uncheckedItems();
    if (items.length === 0) return;

    const url = buildBringImportUrl(
      items.map((item) => ({
        name: item.name,
        amount: item.amount,
        unit: item.unit,
      })),
    );

    // Probe the deep link by navigating to it. If the app handles the URL the
    // browser tab loses visibility (the OS hands off to Bring!); if not, the
    // tab stays visible and we surface the marketing-site fallback.
    if (typeof document === 'undefined' || typeof window === 'undefined') return;

    this.navigate(url);
    this.toast.success('shopping.detail.bring.sent', { count: items.length });

    setTimeout(() => {
      if (document.visibilityState === 'visible') {
        this.toast.warning('shopping.detail.bring.notInstalled', {
          fallbackUrl: BRING_FALLBACK_URL,
        });
      }
    }, BRING_LAUNCH_PROBE_MS);
  }

  // Indirection so tests can stub the navigation; jsdom's location is immutable.
  protected navigate(url: string): void {
    window.location.href = url;
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
