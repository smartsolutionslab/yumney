import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MealPlanApiService, type MealHistoryEntry } from '@yumney/shared/api-client';
import { createAsyncState, ERROR_MAPS } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';

const SEARCH_DEBOUNCE_MS = 250;
const RESULTS_PAGE_SIZE = 50;

@Component({
  selector: 'yn-meal-history',
  standalone: true,
  imports: [
    TranslocoModule,
    ReactiveFormsModule,
    RouterLink,
    LucideAngularModule,
    AsyncStateComponent,
  ],
  templateUrl: './meal-history.component.html',
  styleUrl: './meal-history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealHistoryComponent {
  private api = inject(MealPlanApiService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private searchState = createAsyncState();

  protected term = new FormControl<string>('', { nonNullable: true });
  protected results = signal<MealHistoryEntry[]>([]);
  protected loading = this.searchState.isLoading;
  protected error = this.searchState.serverError;
  protected hasResults = computed(() => this.results().length > 0);

  constructor() {
    this.runSearch('');
    this.term.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => this.runSearch(value));
  }

  protected onSelect(entry: MealHistoryEntry): void {
    const parsed = parseIsoWeek(entry.week);
    if (!parsed) return;
    void this.router.navigate(['/meal-planner'], {
      queryParams: { year: parsed.year, week: parsed.week },
    });
  }

  protected onClear(): void {
    this.term.setValue('');
  }

  protected onRetry(): void {
    this.runSearch(this.term.value);
  }

  private runSearch(value: string): void {
    const trimmed = value.trim();
    this.searchState.execute(
      this.api.searchHistory({
        term: trimmed.length > 0 ? trimmed : undefined,
        pageSize: RESULTS_PAGE_SIZE,
      }),
      ERROR_MAPS.mealPlanner.searchHistory,
      (response) => this.results.set(response.items),
    );
  }
}

function parseIsoWeek(value: string): { year: number; week: number } | null {
  const match = /^(\d{4})-W(\d{2})$/.exec(value);
  if (!match) return null;
  const year = Number(match[1]);
  const week = Number(match[2]);
  if (!Number.isInteger(year) || !Number.isInteger(week)) return null;
  return { year, week };
}
