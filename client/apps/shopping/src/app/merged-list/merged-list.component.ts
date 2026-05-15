import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { ShoppingApiService, type MergedShoppingList, type MergedShoppingItem, type AddedItem, type ItemSource } from '../api';
import { createAsyncState, ERROR_MAPS, groupByCategory, type CategoryGroup, ToastService } from '@yumney/shared/models';
import {
  AsyncStateComponent,
  CATEGORY_KEYS,
  CategorySectionComponent,
  type CategoryKey,
  EmptyStateComponent,
  normalizeCategory,
} from '@yumney/ui';

@Component({
  selector: 'yn-merged-list',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    TranslocoModule,
    LucideAngularModule,
    AsyncStateComponent,
    CategorySectionComponent,
    EmptyStateComponent,
  ],
  templateUrl: './merged-list.component.html',
  styleUrl: './merged-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MergedListComponent {
  private api = inject(ShoppingApiService);
  private destroyRef = inject(DestroyRef);
  private toasts = inject(ToastService);
  private mutationState = createAsyncState();

  protected list = signal<MergedShoppingList | null>(null);
  protected loading = signal(false);
  protected error = signal<string | null>(null);
  protected newItemName = signal('');
  protected showPastPurchases = signal(false);
  protected expandedItems = signal<ReadonlySet<string>>(new Set());

  private loadRequestId = 0;

  protected categoryGroups = computed<CategoryGroup<MergedShoppingItem, CategoryKey>[]>(() => {
    const list = this.list();
    if (!list) return [];
    return groupByCategory(list.items, (item) => normalizeCategory(item.category), {
      order: CATEGORY_KEYS,
    });
  });

  protected totalItems = computed(() => this.list()?.items.length ?? 0);
  protected boughtItems = computed(() => this.list()?.items.filter((item) => item.isBought).length ?? 0);

  constructor() {
    this.loadList();
  }

  protected onAddItem(): void {
    const name = this.newItemName().trim();
    if (!name) return;

    this.newItemName.set('');
    this.mutationState.execute(
      this.api.addItem({ name }),
      ERROR_MAPS.shopping.merged.add,
      (added) => {
        // Read-after-write across an async projection: add lands in the
        // ShoppingLedger event store + Wolverine outbox, then the projection
        // updates ShoppingLedgerReadItem on its own listener thread. A naive
        // GET right after POST returns the pre-add state, so the new item
        // briefly disappears from the UI. Patch the local signal optimistically
        // first, then re-fetch with backoff until the projection catches up
        // and we can replace the optimistic state with server truth.
        this.applyOptimisticAdd(added);
        this.refreshUntilContains(added);
      },
      (errorKey) => this.error.set(errorKey),
    );
  }

  protected onRemoveItem(itemName: string): void {
    this.mutationState.execute(
      this.api.removeItem({ name: itemName }),
      ERROR_MAPS.shopping.merged.remove,
      () => this.loadList(),
      (errorKey) => this.error.set(errorKey),
    );
  }

  protected onExport(): void {
    this.mutationState.execute(
      this.api.exportList(),
      ERROR_MAPS.shopping.merged.export,
      (text) => {
        if (text.trim().length === 0) {
          this.toasts.info('shopping.export.nothing');
          return;
        }

        if (navigator.share) {
          navigator
            .share({ text })
            .then(() => this.toasts.success('shopping.export.shared'))
            .catch((shareError: unknown) => {
              // AbortError = user dismissed share sheet; do not fall back.
              if (shareError instanceof DOMException && shareError.name === 'AbortError') return;
              this.copyToClipboard(text);
            });
        } else {
          this.copyToClipboard(text);
        }
      },
      (errorKey) => this.error.set(errorKey),
    );
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.onAddItem();
    }
  }

  protected onRetry(): void {
    this.loadList();
  }

  protected onTogglePastPurchases(): void {
    this.showPastPurchases.update((current) => !current);
    this.loadList();
  }

  protected onToggleExpanded(item: MergedShoppingItem): void {
    const key = expandKey(item);
    this.expandedItems.update((current) => {
      const next = new Set(current);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
  }

  protected isExpanded(item: MergedShoppingItem): boolean {
    return this.expandedItems().has(expandKey(item));
  }

  // Backend source strings: 'manual', 'chat', or a free-text recipe label.
  // Map the well-known cases to a translation key; pass the rest through
  // as-is for the template to render literally.
  protected sourceTranslationKey(source: ItemSource): string | null {
    const raw = (source.source ?? '').trim().toLowerCase();
    if (raw.length === 0 || raw === 'manual' || raw === 'chat') return 'shopping.sources.manual';
    return null;
  }

  protected sourceLiteral(source: ItemSource): string {
    return source.source ?? '';
  }

  protected formatSourceQuantity(source: ItemSource, item: MergedShoppingItem): string {
    if (item.unit) return `${source.quantity} ${item.unit}`;
    return `${source.quantity}`;
  }

  private loadList(): void {
    const requestId = ++this.loadRequestId;
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getMergedList(this.showPastPurchases())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          if (requestId !== this.loadRequestId) return;
          this.list.set(list);
          this.loading.set(false);
        },
        error: () => {
          if (requestId !== this.loadRequestId) return;
          this.error.set('shopping.errors.loadFailed');
          this.loading.set(false);
        },
      });
  }

  private copyToClipboard(text: string): void {
    navigator.clipboard
      .writeText(text)
      .then(() => this.toasts.success('shopping.export.copied'))
      .catch(() => this.error.set('shopping.errors.exportFailed'));
  }

  private applyOptimisticAdd(added: AddedItem): void {
    const current = this.list() ?? { items: [] };
    const matchIndex = current.items.findIndex(
      (item) => item.itemName.toLowerCase() === added.itemName.toLowerCase() && item.unit === added.unit,
    );

    let nextItems: MergedShoppingItem[];
    if (matchIndex >= 0) {
      const matched = current.items[matchIndex];
      const merged: MergedShoppingItem = {
        ...matched,
        totalQuantity: matched.totalQuantity + added.quantity,
        displayQuantity: matched.displayQuantity + added.quantity,
      };
      nextItems = current.items.map((item, index) => (index === matchIndex ? merged : item));
    } else {
      nextItems = [
        ...current.items,
        {
          itemName: added.itemName,
          totalQuantity: added.quantity,
          displayQuantity: added.quantity,
          unit: added.unit,
          category: added.category,
          isBought: false,
          sources: [],
        },
      ];
    }

    this.list.set({ ...current, items: nextItems });
  }

  private refreshUntilContains(added: AddedItem, attempt = 0): void {
    // Capped-backoff poll. Eight attempts at 200/400/.../1600 ms ≈ 7.2s
    // total — comfortably inside the merged-list test's 60s budget but
    // well under what a user would notice in normal use, where the
    // projection typically lands inside one tick.
    const maxAttempts = 8;
    const requestId = ++this.loadRequestId;

    this.api
      .getMergedList(this.showPastPurchases())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          if (requestId !== this.loadRequestId) return;
          if (this.containsAddedItem(list, added)) {
            this.list.set(list);
            return;
          }

          if (attempt + 1 >= maxAttempts) {
            // Give up reconciling, keep the optimistic state so the user
            // still sees their addition. The next mutation or page load
            // will pick up the canonical truth.
            return;
          }

          setTimeout(() => this.refreshUntilContains(added, attempt + 1), Math.min(1600, 200 * (attempt + 1)));
        },
        error: () => {
          if (requestId !== this.loadRequestId) return;
          this.error.set('shopping.errors.loadFailed');
        },
      });
  }

  private containsAddedItem(list: MergedShoppingList, added: AddedItem): boolean {
    return list.items.some(
      (item) =>
        item.itemName.toLowerCase() === added.itemName.toLowerCase() && item.unit === added.unit && item.totalQuantity >= added.quantity,
    );
  }
}

function expandKey(item: MergedShoppingItem): string {
  return `${item.itemName.toLowerCase()}|${item.unit ?? ''}`;
}
