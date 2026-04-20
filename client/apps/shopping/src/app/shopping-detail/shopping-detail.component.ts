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
import { BackLinkComponent, LoadingSpinnerComponent } from '@yumney/ui';

@Component({
  selector: 'yn-shopping-detail',
  imports: [TranslocoModule, RouterLink, BackLinkComponent, LoadingSpinnerComponent],
  templateUrl: './shopping-detail.component.html',
  styleUrl: './shopping-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingDetailComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private shoppingApi = inject(ShoppingApiService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);

  shoppingList = signal<ShoppingListDetail | null>(null);
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;

  checkedCount = computed(() => this.shoppingList()?.items.filter((i) => i.isChecked).length ?? 0);

  totalItemCount = computed(() => this.shoppingList()?.items.length ?? 0);

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

  onCheckAll(): void {
    this.optimisticBatchUpdate(true);
  }

  onReset(): void {
    this.optimisticBatchUpdate(false);
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
