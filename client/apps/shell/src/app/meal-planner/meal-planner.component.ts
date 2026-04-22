import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  signal,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  MealPlanApiService,
  type WeeklyPlan,
  type MealSlot,
  type GenerateShoppingListResult,
} from '@yumney/shared/api-client';
import { UI } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';

const WEEKS_PER_YEAR = 52;
const MS_PER_DAY = 86_400_000;
const SWIPE_MIN_DX_PX = 60;
const SWIPE_MAX_DY_RATIO = 0.5; // |dy| must be < 50% of |dx| to count as horizontal
const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const JS_DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

@Component({
  selector: 'yn-meal-planner',
  standalone: true,
  imports: [TranslocoModule, LucideAngularModule, AsyncStateComponent],
  templateUrl: './meal-planner.component.html',
  styleUrl: './meal-planner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlannerComponent {
  private api = inject(MealPlanApiService);
  private destroyRef = inject(DestroyRef);
  private transloco = inject(TranslocoService);
  private router = inject(Router);
  private host = inject(ElementRef<HTMLElement>);

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

  protected days = DAY_NAMES;

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
      this.weekNumber.set(WEEKS_PER_YEAR);
    } else {
      this.weekNumber.update((w) => w - 1);
    }
    this.loadPlan();
  }

  protected onNextWeek(): void {
    if (this.weekNumber() >= WEEKS_PER_YEAR) {
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

  protected onSelectSlot(day: string): void {
    this.router.navigate(['/recipes'], {
      queryParams: {
        assignTo: `${this.year()}-W${String(this.weekNumber()).padStart(2, '0')}-${day}`,
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
          queueMicrotask(() => this.scrollTodayIntoView());
        },
        error: () => {
          this.error.set(this.transloco.translate('mealPlanner.errors.loadFailed'));
          this.loading.set(false);
        },
      });
  }

  private scrollTodayIntoView(): void {
    const el = this.host.nativeElement.querySelector<HTMLElement>('.day-card.today');
    el?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }

  protected onRetry(): void {
    this.loadPlan();
  }

  private swipeStart: { x: number; y: number } | null = null;

  @HostListener('pointerdown', ['$event'])
  protected onSwipeStart(event: PointerEvent): void {
    if (event.pointerType !== 'touch') return;
    this.swipeStart = { x: event.clientX, y: event.clientY };
  }

  @HostListener('pointerup', ['$event'])
  protected onSwipeEnd(event: PointerEvent): void {
    const start = this.swipeStart;
    this.swipeStart = null;
    if (!start || event.pointerType !== 'touch') return;

    const dx = event.clientX - start.x;
    const dy = event.clientY - start.y;
    if (Math.abs(dx) < SWIPE_MIN_DX_PX) return;
    if (Math.abs(dy) > Math.abs(dx) * SWIPE_MAX_DY_RATIO) return;

    if (dx < 0) {
      this.onNextWeek();
    } else {
      this.onPreviousWeek();
    }
  }

  protected isToday(dayName: string): boolean {
    return JS_DAY_NAMES[new Date().getDay()] === dayName;
  }

  private getCurrentWeek(): number {
    const now = new Date();
    const jan4 = new Date(now.getFullYear(), 0, 4);
    const daysDiff = Math.floor((now.getTime() - jan4.getTime()) / MS_PER_DAY);
    return Math.ceil((daysDiff + jan4.getDay() + 1) / 7);
  }
}
