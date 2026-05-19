import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, map, Observable, of, tap } from 'rxjs';
import { DashboardApiService, type SuggestionsResponse, type UserActivityItem } from '@yumney/shared/api-dashboard';
import type { QuickAction } from '@yumney/ui';

@Injectable()
export class DashboardSuggestionsService {
  private api = inject(DashboardApiService);
  private destroyRef = inject(DestroyRef);

  readonly quickActions = signal<QuickAction[]>([]);
  readonly suggestions = signal<SuggestionsResponse | null>(null);
  readonly recentActivity = signal<UserActivityItem[]>([]);
  readonly loading = signal(true);

  load(): Observable<{ initialDataIsEmpty: boolean }> {
    this.api
      .getRecentActivity(5)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (activity) => this.recentActivity.set(activity),
        error: () => this.recentActivity.set([]),
      });

    return this.api.getSuggestions().pipe(
      tap({
        next: (response) => {
          this.suggestions.set(response);
          this.quickActions.set(
            response.quickActions.map((key) => ({
              key,
              labelKey: 'dashboard.quickActions.' + key,
            })),
          );
          this.loading.set(false);
        },
      }),
      map((response) => ({
        initialDataIsEmpty: response.quickActions.length === 0 && response.suggestions.length === 0,
      })),
      catchError(() => {
        // Keep the loading spinner from spinning forever if the API fails,
        // and surface the same empty-state behaviour as a successful empty response.
        this.loading.set(false);
        return of({ initialDataIsEmpty: true });
      }),
      takeUntilDestroyed(this.destroyRef),
    );
  }
}
