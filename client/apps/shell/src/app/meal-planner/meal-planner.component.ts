import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  MealPlanApiService,
  type WeeklyPlan,
  type MealSlot,
  type GenerateShoppingListResult,
} from '@yumney/shared/api-client';
import { UI } from '@yumney/shared/models';

@Component({
  selector: 'yn-meal-planner',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule],
  templateUrl: './meal-planner.component.html',
  styleUrl: './meal-planner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlannerComponent {
  private api = inject(MealPlanApiService);
  private destroyRef = inject(DestroyRef);
  private transloco = inject(TranslocoService);

  protected year = signal(new Date().getFullYear());
  protected weekNumber = signal(this.getCurrentWeek());
  protected plan = signal<WeeklyPlan | null>(null);
  protected loading = signal(false);
  protected error = signal<string | null>(null);
  protected generatingList = signal(false);
  protected shoppingResult = signal<GenerateShoppingListResult | null>(null);

  protected weekLabel = computed(
    () => `${this.year()}-W${String(this.weekNumber()).padStart(2, '0')}`,
  );

  protected days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  protected dinnerSlots = computed(() => {
    const p = this.plan();
    if (!p) return this.days.map((d) => ({ day: d, slot: null as MealSlot | null }));
    return this.days.map((d) => ({
      day: d,
      slot: p.slots.find((s) => s.day === d && s.mealType === 'Dinner') ?? null,
    }));
  });

  constructor() {
    this.loadPlan();
  }

  protected onPreviousWeek(): void {
    if (this.weekNumber() <= 1) {
      this.year.update((y) => y - 1);
      this.weekNumber.set(52);
    } else {
      this.weekNumber.update((w) => w - 1);
    }
    this.loadPlan();
  }

  protected onNextWeek(): void {
    if (this.weekNumber() >= 52) {
      this.year.update((y) => y + 1);
      this.weekNumber.set(1);
    } else {
      this.weekNumber.update((w) => w + 1);
    }
    this.loadPlan();
  }

  protected hasRecipeSlots = computed(
    () => this.plan()?.slots.some((s) => s.contentType === 'Recipe') ?? false,
  );

  protected onGenerateShoppingList(): void {
    this.generatingList.set(true);
    this.shoppingResult.set(null);
    this.error.set(null);

    this.api
      .generateShoppingList(this.year(), this.weekNumber())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.shoppingResult.set(result);
          this.generatingList.set(false);
          setTimeout(() => this.shoppingResult.set(null), UI.RESULT_DISPLAY_MS);
        },
        error: () => {
          this.error.set(this.transloco.translate('mealPlanner.errors.generateFailed'));
          this.generatingList.set(false);
        },
      });
  }

  protected onClearSlot(day: string): void {
    this.api
      .clearSlot(this.year(), this.weekNumber(), { day })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (plan) => this.plan.set(plan),
        error: () => this.error.set(this.transloco.translate('mealPlanner.errors.clearFailed')),
      });
  }

  private loadPlan(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getWeeklyPlan(this.year(), this.weekNumber())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (plan) => {
          this.plan.set(plan);
          this.loading.set(false);
        },
        error: () => {
          this.error.set(this.transloco.translate('mealPlanner.errors.loadFailed'));
          this.loading.set(false);
        },
      });
  }

  protected onRetry(): void {
    this.loadPlan();
  }

  protected isToday(dayName: string): boolean {
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    return days[new Date().getDay()] === dayName;
  }

  private getCurrentWeek(): number {
    const now = new Date();
    const jan4 = new Date(now.getFullYear(), 0, 4);
    const daysDiff = Math.floor((now.getTime() - jan4.getTime()) / 86400000);
    return Math.ceil((daysDiff + jan4.getDay() + 1) / 7);
  }
}
