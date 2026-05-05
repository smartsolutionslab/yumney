import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
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
  private destroyRef = inject(DestroyRef);
  private state = createAsyncState(this.destroyRef);

  protected loading = this.state.isLoading;
  protected error = this.state.serverError;
  protected entries = signal<UserActivityItem[]>([]);
  protected activeFilter = signal<ActivityTypeKey | 'all'>('all');

  protected timelineEntries = computed<ActivityEntry[]>(() =>
    this.entries().map((entry) => ({
      type: entry.type,
      recipeIdentifier: entry.recipeIdentifier,
      recipeTitle: entry.recipeTitle,
      occurredAt: entry.occurredAt,
    })),
  );

  constructor() {
    this.load();
  }

  protected onFilterChange(value: ActivityTypeKey | 'all'): void {
    if (this.activeFilter() === value) return;
    this.activeFilter.set(value);
    this.load();
  }

  protected onRetry(): void {
    this.load();
  }

  private load(): void {
    const type = this.activeFilter();
    const options = type === 'all' ? { limit: 50 } : { limit: 50, type };
    this.state.execute(this.api.getActivity(options), ERROR_MAPS.account.load, (entries) =>
      this.entries.set(entries),
    );
  }
}
