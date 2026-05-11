import {
  Component,
  ChangeDetectionStrategy,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  ShoppingApiService,
  type Freshness,
  type IngredientBalanceItem,
  type MarkAsFrozenRequest,
} from '../api';
import { createAsyncState, ERROR_MAPS, groupByCategory } from '@yumney/shared/models';
import { EmptyStateComponent, MessageBannerComponent } from '@yumney/ui';

@Component({
  selector: 'yn-pantry',
  imports: [TranslocoModule, LucideAngularModule, EmptyStateComponent, MessageBannerComponent],
  templateUrl: './pantry.component.html',
  styleUrl: './pantry.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PantryComponent implements OnInit {
  private shoppingApi = inject(ShoppingApiService);
  private destroyRef = inject(DestroyRef);
  private loadState = createAsyncState(this.destroyRef);
  private freezeState = createAsyncState(this.destroyRef);

  items = signal<IngredientBalanceItem[]>([]);
  isLoading = this.loadState.isLoading;
  serverError = computed(() => this.freezeState.serverError() ?? this.loadState.serverError());

  // The balance endpoint returns items in no particular order; group them by
  // category for the merged-list-style "sections per area of the kitchen"
  // layout. groupByCategory takes a key function so we don't need to reshape
  // the items.
  groups = computed(() => groupByCategory(this.items(), (item) => item.category));

  ngOnInit(): void {
    this.load();
  }

  // Visual nudge tier — drives the color chip per row. Backend Freshness
  // values map directly; NotTracked (staples + pantry items) hides the chip.
  freshnessTone(freshness: Freshness): 'fresh' | 'use-soon' | 'check-it' | null {
    switch (freshness) {
      case 'Fresh':
        return 'fresh';
      case 'UseSoon':
        return 'use-soon';
      case 'CheckIt':
        return 'check-it';
      default:
        return null;
    }
  }

  onFreeze(item: IngredientBalanceItem): void {
    const request: MarkAsFrozenRequest = { name: item.itemName, unit: item.unit };
    this.freezeState.execute(
      this.shoppingApi.markAsFrozen(request),
      ERROR_MAPS.shopping.pantry.freeze,
      () => this.load(),
    );
  }

  onRetry(): void {
    this.load();
  }

  private load(): void {
    this.loadState.execute(
      this.shoppingApi.getIngredientBalance(),
      ERROR_MAPS.shopping.pantry.load,
      (response) => this.items.set(response.items),
    );
  }
}
