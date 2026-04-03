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
import { BackLinkComponent, LoadingSpinnerComponent } from '@yumney/ui';

@Component({
  selector: 'yn-shopping-detail',
  imports: [TranslocoModule, RouterLink, BackLinkComponent, LoadingSpinnerComponent],
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
      ShoppingDetailComponent.errorMap,
      (list) => this.shoppingList.set(list),
    );
  }

  onToggleItem(item: ShoppingListItemResponse): void {
    const previousState = item.isChecked;
    this.optimisticUpdate(
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
    if (!list) {
      return;
    }

    const previousStates = list.items.map((i) => i.isChecked);
    this.optimisticUpdate(
      () => {
        for (const item of list.items) item.isChecked = checked;
      },
      () => {
        list.items.forEach((item, i) => (item.isChecked = previousStates[i]));
      },
      (l) => this.shoppingApi.checkOffAllItems(l.identifier, checked),
    );
  }

  private optimisticUpdate(
    apply: () => void,
    rollback: () => void,
    apiCall: (list: ShoppingListDetail) => import('rxjs').Observable<unknown>,
  ): void {
    const list = this.shoppingList();
    if (!list) {
      return;
    }

    apply();
    this.shoppingList.set({ ...list });

    apiCall(list)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        error: () => {
          rollback();
          this.shoppingList.set({ ...list });
        },
      });
  }
}
