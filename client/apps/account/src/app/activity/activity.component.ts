import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { createAsyncState, ERROR_MAPS } from '@yumney/shared/models';
import { ActivityTimelineComponent, AsyncStateComponent, type ActivityEntry } from '@yumney/ui';
import { ActivityApiService, type ActivityTypeKey, type UserActivityItem } from '../api';

const FILTER_OPTIONS: ReadonlyArray<{ value: ActivityTypeKey | 'all'; labelKey: string }> = [
  { value: 'all', labelKey: 'account.activity.filter.all' },
  { value: 'recipe_imported', labelKey: 'account.activity.filter.imported' },
  { value: 'recipe_viewed', labelKey: 'account.activity.filter.viewed' },
  { value: 'recipe_cooked', labelKey: 'account.activity.filter.cooked' },
  { value: 'recipe_edited', labelKey: 'account.activity.filter.edited' },
  { value: 'recipe_deleted', labelKey: 'account.activity.filter.deleted' },
];

const PAGE_SIZE = 20;

@Component({
  selector: 'yn-activity-page',
  standalone: true,
  imports: [TranslocoModule, AsyncStateComponent, ActivityTimelineComponent],
  templateUrl: './activity.component.html',
  styleUrl: './activity.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityComponent {
  protected readonly filterOptions = FILTER_OPTIONS;

  private api = inject(ActivityApiService);
  private state = createAsyncState();

  protected loading = this.state.isLoading;
  protected error = this.state.serverError;
  protected entries = signal<UserActivityItem[]>([]);
  protected activeFilter = signal<ActivityTypeKey | 'all'>('all');
  protected nextCursor = signal<string | null>(null);
  protected hasMore = computed(() => this.nextCursor() !== null);

  protected timelineEntries = computed<ActivityEntry[]>(() =>
    this.entries().map((entry) => ({
      type: entry.type,
      recipeIdentifier: entry.recipeIdentifier,
      recipeTitle: entry.recipeTitle,
      occurredAt: entry.occurredAt,
    })),
  );

  constructor() {
    this.loadFirstPage();
  }

  protected onFilterChange(value: ActivityTypeKey | 'all'): void {
    if (this.activeFilter() === value) return;
    this.activeFilter.set(value);
    this.loadFirstPage();
  }

  protected onRetry(): void {
    this.loadFirstPage();
  }

  protected onLoadMore(): void {
    const cursor = this.nextCursor();
    if (cursor === null) return;

    const type = this.activeFilter();
    const options =
      type === 'all' ? { limit: PAGE_SIZE, cursor } : { limit: PAGE_SIZE, type, cursor };

    this.state.execute(this.api.getActivity(options), ERROR_MAPS.account.load, (page) => {
      this.entries.update((current) => [...current, ...page.items]);
      this.nextCursor.set(page.nextCursor);
    });
  }

  private loadFirstPage(): void {
    const type = this.activeFilter();
    const options = type === 'all' ? { limit: PAGE_SIZE } : { limit: PAGE_SIZE, type };

    this.state.execute(this.api.getActivity(options), ERROR_MAPS.account.load, (page) => {
      this.entries.set(page.items);
      this.nextCursor.set(page.nextCursor);
    });
  }
}
