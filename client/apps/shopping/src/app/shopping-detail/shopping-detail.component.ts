import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import {
  ShoppingApiService,
  ShoppingListDetail,
  ShoppingListItemResponse,
} from '@yumney/shared/api-client';
import { createAsyncState, HttpErrorMap } from '@yumney/shared/models';

@Component({
  selector: 'yn-shopping-detail',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './shopping-detail.component.html',
  styleUrl: './shopping-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShoppingDetailComponent implements OnInit {
  private static readonly errorMap: HttpErrorMap = {
    404: 'shopping.detail.errors.notFound',
    default: 'shopping.detail.errors.generic',
  };

  private shoppingApi = inject(ShoppingApiService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);

  shoppingList = signal<ShoppingListDetail | null>(null);
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;

  checkedCount = computed(
    () => this.shoppingList()?.items.filter((i) => i.isChecked).length ?? 0,
  );

  totalItemCount = computed(() => this.shoppingList()?.items.length ?? 0);

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('shopping.detail.errors.notFound');
      return;
    }

    this.asyncState.execute(
      this.shoppingApi.getShoppingListById(identifier),
      ShoppingDetailComponent.errorMap,
      (list) => this.shoppingList.set(list),
    );
  }

  onToggleItem(item: ShoppingListItemResponse): void {
    const list = this.shoppingList();
    if (!list) {
      return;
    }

    const previousState = item.isChecked;
    item.isChecked = !previousState;
    this.shoppingList.set({ ...list });

    this.shoppingApi
      .checkOffItem(list.identifier, item.identifier, !previousState)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          item.isChecked = previousState;
          this.shoppingList.set({ ...list });
        },
      });
  }

  onCheckAll(): void {
    const list = this.shoppingList();
    if (!list) {
      return;
    }

    const previousStates = list.items.map((i) => i.isChecked);
    list.items.forEach((item) => (item.isChecked = true));
    this.shoppingList.set({ ...list });

    this.shoppingApi
      .checkOffAllItems(list.identifier, true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          list.items.forEach((item, i) => (item.isChecked = previousStates[i]));
          this.shoppingList.set({ ...list });
        },
      });
  }

  onReset(): void {
    const list = this.shoppingList();
    if (!list) {
      return;
    }

    const previousStates = list.items.map((i) => i.isChecked);
    list.items.forEach((item) => (item.isChecked = false));
    this.shoppingList.set({ ...list });

    this.shoppingApi
      .checkOffAllItems(list.identifier, false)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          list.items.forEach((item, i) => (item.isChecked = previousStates[i]));
          this.shoppingList.set({ ...list });
        },
      });
  }
}
