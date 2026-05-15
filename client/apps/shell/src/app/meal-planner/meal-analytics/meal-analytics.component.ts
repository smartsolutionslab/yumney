import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { MealPlanApiService, type MealAnalytics } from '@yumney/shared/api-client';
import { createAsyncState, ERROR_MAPS } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';

type ViewMode = 'month' | 'year';

const MONTHS = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12] as const;

const DONUT_RADIUS = 56;
const DONUT_CIRCUMFERENCE = 2 * Math.PI * DONUT_RADIUS;

@Component({
  selector: 'yn-meal-analytics',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule, RouterLink, AsyncStateComponent],
  templateUrl: './meal-analytics.component.html',
  styleUrl: './meal-analytics.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealAnalyticsComponent {
  private api = inject(MealPlanApiService);
  private loadState = createAsyncState();

  protected viewMode = signal<ViewMode>('month');
  protected year = signal(new Date().getFullYear());
  protected month = signal(new Date().getMonth() + 1);
  protected analytics = signal<MealAnalytics | null>(null);

  protected loading = this.loadState.isLoading;
  protected error = this.loadState.serverError;

  protected months = MONTHS;

  protected periodLabel = computed(() => {
    if (this.viewMode() === 'year') return String(this.year());
    return `${this.year()}-${String(this.month()).padStart(2, '0')}`;
  });

  protected donutSegments = computed(() => {
    const distribution = this.analytics()?.categoryDistribution ?? [];
    let offset = 0;
    return distribution.map((share) => {
      const length = (share.percentage / 100) * DONUT_CIRCUMFERENCE;
      const segment = {
        category: share.category,
        count: share.count,
        percentage: share.percentage,
        dashArray: `${length} ${DONUT_CIRCUMFERENCE - length}`,
        dashOffset: -offset,
      };
      offset += length;
      return segment;
    });
  });

  protected circumference = DONUT_CIRCUMFERENCE;
  protected donutRadius = DONUT_RADIUS;

  constructor() {
    this.loadAnalytics();
  }

  protected onSetView(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.loadAnalytics();
  }

  protected onPreviousPeriod(): void {
    if (this.viewMode() === 'year') {
      this.year.update((year) => year - 1);
    } else if (this.month() <= 1) {
      this.year.update((year) => year - 1);
      this.month.set(12);
    } else {
      this.month.update((value) => value - 1);
    }
    this.loadAnalytics();
  }

  protected onNextPeriod(): void {
    if (this.viewMode() === 'year') {
      this.year.update((year) => year + 1);
    } else if (this.month() >= 12) {
      this.year.update((year) => year + 1);
      this.month.set(1);
    } else {
      this.month.update((value) => value + 1);
    }
    this.loadAnalytics();
  }

  protected onRetry(): void {
    this.loadAnalytics();
  }

  private loadAnalytics(): void {
    const month = this.viewMode() === 'month' ? this.month() : undefined;
    this.loadState.execute(
      this.api.getMealAnalytics(this.year(), month),
      ERROR_MAPS.mealPlanner.analytics,
      (result) => this.analytics.set(result),
    );
  }
}
