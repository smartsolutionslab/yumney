import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { MealPlanApiService, type WeeklyPlan, type MealSlot } from '@yumney/shared/api-client';

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

  protected year = signal(new Date().getFullYear());
  protected weekNumber = signal(this.getCurrentWeek());
  protected plan = signal<WeeklyPlan | null>(null);
  protected loading = signal(false);
  protected error = signal<string | null>(null);

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

  protected onClearSlot(day: string): void {
    this.api
      .clearSlot(this.year(), this.weekNumber(), { day })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (plan) => this.plan.set(plan),
        error: () => this.error.set('Failed to clear slot'),
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
          this.error.set('Failed to load meal plan');
          this.loading.set(false);
        },
      });
  }

  protected isToday(dayName: string): boolean {
    const dayMap: Record<string, number> = {
      Sunday: 0,
      Monday: 1,
      Tuesday: 2,
      Wednesday: 3,
      Thursday: 4,
      Friday: 5,
      Saturday: 6,
    };
    return dayMap[dayName] === new Date().getDay();
  }

  private getCurrentWeek(): number {
    const now = new Date();
    const start = new Date(now.getFullYear(), 0, 1);
    const diff = now.getTime() - start.getTime();
    const oneWeek = 604800000;
    return Math.ceil((diff / oneWeek + start.getDay() + 1) / 7);
  }
}
