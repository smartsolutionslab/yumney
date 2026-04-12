import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  ShoppingApiService,
  type MergedShoppingList,
  type MergedShoppingItem,
} from '@yumney/shared/api-client';

interface CategoryGroup {
  category: string;
  items: MergedShoppingItem[];
  isExpanded: boolean;
}

@Component({
  selector: 'yn-merged-list',
  standalone: true,
  imports: [FormsModule, TranslocoModule, LucideAngularModule],
  templateUrl: './merged-list.component.html',
  styleUrl: './merged-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MergedListComponent {
  private api = inject(ShoppingApiService);
  private destroyRef = inject(DestroyRef);

  protected list = signal<MergedShoppingList | null>(null);
  protected loading = signal(false);
  protected error = signal<string | null>(null);
  protected newItemName = signal('');

  protected categoryGroups = computed<CategoryGroup[]>(() => {
    const l = this.list();
    if (!l) return [];

    const grouped = new Map<string, MergedShoppingItem[]>();
    for (const item of l.items) {
      const existing = grouped.get(item.category) ?? [];
      existing.push(item);
      grouped.set(item.category, existing);
    }

    return Array.from(grouped.entries()).map(([category, items]) => ({
      category,
      items,
      isExpanded: true,
    }));
  });

  protected totalItems = computed(() => this.list()?.items.length ?? 0);
  protected boughtItems = computed(() => this.list()?.items.filter((i) => i.isBought).length ?? 0);

  constructor() {
    this.loadList();
  }

  protected onAddItem(): void {
    const name = this.newItemName().trim();
    if (!name) return;

    this.newItemName.set('');
    this.api
      .addItem({ name })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadList(),
        error: () => this.error.set('Failed to add item'),
      });
  }

  protected onRemoveItem(itemName: string): void {
    this.api
      .removeItem({ name: itemName })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.loadList(),
        error: () => this.error.set('Failed to remove item'),
      });
  }

  protected onExport(): void {
    this.api
      .exportList()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (text) => {
          if (navigator.share) {
            navigator.share({ text }).catch(() => this.copyToClipboard(text));
          } else {
            this.copyToClipboard(text);
          }
        },
        error: () => this.error.set('Failed to export'),
      });
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.onAddItem();
    }
  }

  protected onRetry(): void {
    this.loadList();
  }

  protected getCategoryLabel(category: string): string {
    const labels: Record<string, string> = {
      produce: '🥦 Produce',
      dairy: '🥛 Dairy',
      'meat-fish': '🥩 Meat & Fish',
      bakery: '🍞 Bakery',
      frozen: '❄️ Frozen',
      beverages: '🥤 Beverages',
      pantry: '🏠 Pantry',
      household: '🧹 Household',
      other: '📦 Other',
    };
    return labels[category] ?? category;
  }

  private loadList(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getMergedList()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => {
          this.list.set(list);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load shopping list');
          this.loading.set(false);
        },
      });
  }

  private copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text);
  }
}
