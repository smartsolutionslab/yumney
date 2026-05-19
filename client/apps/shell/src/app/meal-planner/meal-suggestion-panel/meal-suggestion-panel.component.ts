import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { concat, last, toArray } from 'rxjs';
import { MealPlanApiService, type WeekSuggestion, type WeekSuggestionEntry } from '@yumney/shared/api-client';
import { createAsyncState, ERROR_MAPS } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';

@Component({
  selector: 'yn-meal-suggestion-panel',
  imports: [TranslocoModule, LucideAngularModule, AsyncStateComponent],
  templateUrl: './meal-suggestion-panel.component.html',
  styleUrl: './meal-suggestion-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealSuggestionPanelComponent {
  private api = inject(MealPlanApiService);
  private suggestState = createAsyncState();
  private acceptState = createAsyncState();

  readonly year = input.required<number>();
  readonly week = input.required<number>();

  readonly planAccepted = output<void>();

  protected suggestion = signal<WeekSuggestion | null>(null);
  protected loading = this.suggestState.isLoading;
  protected accepting = this.acceptState.isLoading;
  protected error = computed(() => this.suggestState.serverError() ?? this.acceptState.serverError());
  protected hasSuggestion = computed(() => (this.suggestion()?.entries.length ?? 0) > 0);

  protected onSuggest(): void {
    this.suggestion.set(null);
    this.suggestState.execute(this.api.suggestWeekPlan(this.year(), this.week()), ERROR_MAPS.mealPlanner.suggestWeek, (result) =>
      this.suggestion.set(result),
    );
  }

  protected onRegenerate(): void {
    this.onSuggest();
  }

  protected onAccept(): void {
    const entries = this.suggestion()?.entries ?? [];
    if (entries.length === 0) return;

    // Sequential: the WeeklyPlan aggregate is event-sourced with optimistic
    // concurrency, so parallel assigns to the same week throw
    // ConcurrencyConflictException on all but the first write. `concat`
    // chains the assigns in order; `last` waits for the final response.
    const assignments = entries.map((entry) =>
      this.api.assignRecipe(this.year(), this.week(), {
        day: entry.day,
        recipeIdentifier: entry.recipeIdentifier,
        recipeTitle: entry.recipeTitle,
        mealType: entry.mealType,
      }),
    );

    this.acceptState.execute(concat(...assignments).pipe(toArray(), last()), ERROR_MAPS.mealPlanner.assign, () => {
      this.suggestion.set(null);
      this.planAccepted.emit();
    });
  }

  protected onDismiss(): void {
    this.suggestion.set(null);
  }

  protected onRetry(): void {
    this.onSuggest();
  }

  protected trackByEntry(_: number, entry: WeekSuggestionEntry): string {
    return entry.day + entry.recipeIdentifier;
  }
}
