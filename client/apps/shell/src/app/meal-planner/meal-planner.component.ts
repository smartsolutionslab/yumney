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
import { Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  MealPlanApiService,
  type WeeklyPlan,
  type MealSlot,
  type GenerateShoppingListResult,
} from '@yumney/shared/api-client';
import { createAsyncState, ERROR_MAPS, UI } from '@yumney/shared/models';
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
  private router = inject(Router);
  private host = inject(ElementRef<HTMLElement>);
  private loadState = createAsyncState(this.destroyRef);
  private generateState = createAsyncState(this.destroyRef);
  private clearSlotState = createAsyncState(this.destroyRef);

  protected year = signal(new Date().getFullYear());
  protected weekNumber = signal(this.getCurrentWeek());
  protected plan = signal<WeeklyPlan | null>(null);
  protected loading = this.loadState.isLoading;
  protected error = computed(
    () =>
      this.loadState.serverError() ??
      this.generateState.serverError() ??
      this.clearSlotState.serverError(),
  );
  protected generatingList = this.generateState.isLoading;
  protected shoppingResult = signal<GenerateShoppingListResult | null>(null);

  protected weekLabel = computed(
    () => `${this.year()}-W${String(this.weekNumber()).padStart(2, '0')}`,
  );

  protected days = DAY_NAMES;

  protected dinnerSlots = computed(() => {
    const plan = this.plan();
    if (!plan) return this.days.map((day) => ({ day, slot: null as MealSlot | null }));
    return this.days.map((day) => ({
      day,
      slot: plan.slots.find((slot) => slot.day === day && slot.mealType === 'Dinner') ?? null,
    }));
  });

  constructor() {
    this.loadPlan();
  }

  protected onPreviousWeek(): void {
    if (this.weekNumber() <= 1) {
      this.year.update((year) => year - 1);
      this.weekNumber.set(WEEKS_PER_YEAR);
    } else {
      this.weekNumber.update((week) => week - 1);
    }
    this.loadPlan();
  }

  protected onNextWeek(): void {
    if (this.weekNumber() >= WEEKS_PER_YEAR) {
      this.year.update((year) => year + 1);
      this.weekNumber.set(1);
    } else {
      this.weekNumber.update((week) => week + 1);
    }
    this.loadPlan();
  }

  protected hasRecipeSlots = computed(
    () => this.plan()?.slots.some((slot) => slot.contentType === 'Recipe') ?? false,
  );

  protected onGenerateShoppingList(): void {
    this.shoppingResult.set(null);

    this.generateState.execute(
      this.api.generateShoppingList(this.year(), this.weekNumber()),
      ERROR_MAPS.mealPlanner.generateShoppingList,
      (result) => {
        this.shoppingResult.set(result);
        setTimeout(() => this.shoppingResult.set(null), UI.RESULT_DISPLAY_MS);
      },
    );
  }

  protected onSelectSlot(day: string): void {
    void this.router.navigate(['/recipes'], {
      queryParams: {
        assignTo: `${this.year()}-W${String(this.weekNumber()).padStart(2, '0')}-${day}`,
      },
    });
  }

  protected onClearSlot(day: string): void {
    this.clearSlotState.execute(
      this.api.clearSlot(this.year(), this.weekNumber(), { day }),
      ERROR_MAPS.mealPlanner.clearSlot,
      (plan) => this.plan.set(plan),
    );
  }

  private loadPlan(): void {
    this.loadState.execute(
      this.api.getWeeklyPlan(this.year(), this.weekNumber()),
      ERROR_MAPS.mealPlanner.load,
      (plan) => {
        this.plan.set(plan);
        queueMicrotask(() => this.scrollTodayIntoView());
      },
    );
  }

  private scrollTodayIntoView(): void {
    const host = this.host.nativeElement as HTMLElement;
    const el = host.querySelector('.day-card.today') as HTMLElement | null;
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
