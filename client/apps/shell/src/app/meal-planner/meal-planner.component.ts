import {
  Component,
  ChangeDetectionStrategy,
  ElementRef,
  HostListener,
  inject,
  signal,
  computed,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  MealPlanApiService,
  type WeeklyPlan,
  type MealSlot,
  type GenerateShoppingListResult,
} from '@yumney/shared/api-client';
import { injectAsyncStates, ERROR_MAPS, UI } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';
import { MealSuggestionPanelComponent } from './meal-suggestion-panel/meal-suggestion-panel.component';

const WEEKS_PER_YEAR = 52;
const MS_PER_DAY = 86_400_000;
const SWIPE_MIN_DX_PX = 60;
const SWIPE_MAX_DY_RATIO = 0.5; // |dy| must be < 50% of |dx| to count as horizontal
const DAY_NAMES = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
const JS_DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

@Component({
  selector: 'yn-meal-planner',
  standalone: true,
  imports: [
    TranslocoModule,
    LucideAngularModule,
    AsyncStateComponent,
    MealSuggestionPanelComponent,
  ],
  templateUrl: './meal-planner.component.html',
  styleUrl: './meal-planner.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealPlannerComponent {
  private api = inject(MealPlanApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private host = inject(ElementRef<HTMLElement>);
  private states = injectAsyncStates('load', 'generate', 'clearSlot', 'copy');

  protected year = signal(new Date().getFullYear());
  protected weekNumber = signal(this.getCurrentWeek());
  protected plan = signal<WeeklyPlan | null>(null);
  protected loading = this.states.load.isLoading;
  protected error = computed(
    () =>
      this.states.load.serverError() ??
      this.states.generate.serverError() ??
      this.states.clearSlot.serverError() ??
      this.states.copy.serverError(),
  );
  protected generatingList = this.states.generate.isLoading;
  protected copyingToCurrent = this.states.copy.isLoading;
  protected shoppingResult = signal<GenerateShoppingListResult | null>(null);

  protected weekLabel = computed(
    () => `${this.year()}-W${String(this.weekNumber()).padStart(2, '0')}`,
  );

  private currentYear = new Date().getFullYear();
  private currentWeekNumber = this.getCurrentWeek();

  protected isPastWeek = computed(() => {
    const year = this.year();
    if (year < this.currentYear) return true;
    if (year > this.currentYear) return false;
    return this.weekNumber() < this.currentWeekNumber;
  });

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
    const queryParams = this.route.snapshot.queryParamMap;
    const yearParam = Number(queryParams.get('year'));
    const weekParam = Number(queryParams.get('week'));
    if (Number.isInteger(yearParam) && yearParam > 0) this.year.set(yearParam);
    if (Number.isInteger(weekParam) && weekParam > 0 && weekParam <= WEEKS_PER_YEAR) {
      this.weekNumber.set(weekParam);
    }
    this.loadPlan();
  }

  protected onOpenHistory(): void {
    void this.router.navigate(['/meal-planner/history']);
  }

  protected onOpenAnalytics(): void {
    void this.router.navigate(['/meal-planner/analytics']);
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

  protected canSuggest = computed(
    () => this.plan() !== null && !this.isPastWeek() && !this.hasRecipeSlots(),
  );

  protected onSuggestionAccepted(): void {
    this.loadPlan();
  }

  protected onGenerateShoppingList(): void {
    this.shoppingResult.set(null);

    this.states.generate.execute(
      this.api.generateShoppingList(this.year(), this.weekNumber()),
      ERROR_MAPS.mealPlanner.generateShoppingList,
      (result) => {
        this.shoppingResult.set(result);
        setTimeout(() => this.shoppingResult.set(null), UI.RESULT_DISPLAY_MS);
      },
    );
  }

  protected onSelectSlot(day: string): void {
    if (this.isPastWeek()) return;
    void this.router.navigate(['/recipes'], {
      queryParams: {
        assignTo: `${this.year()}-W${String(this.weekNumber()).padStart(2, '0')}-${day}`,
      },
    });
  }

  protected onClearSlot(day: string): void {
    if (this.isPastWeek()) return;
    this.states.clearSlot.execute(
      this.api.clearSlot(this.year(), this.weekNumber(), { day }),
      ERROR_MAPS.mealPlanner.clearSlot,
      (plan) => this.plan.set(plan),
    );
  }

  protected onCopyToCurrentWeek(): void {
    if (!this.isPastWeek()) return;
    const srcYear = this.year();
    const srcWeek = this.weekNumber();
    this.states.copy.execute(
      this.api.copyPlanToWeek(srcYear, srcWeek, this.currentYear, this.currentWeekNumber),
      ERROR_MAPS.mealPlanner.copyToWeek,
      () => {
        this.year.set(this.currentYear);
        this.weekNumber.set(this.currentWeekNumber);
        this.loadPlan();
      },
    );
  }

  private loadPlan(): void {
    this.states.load.execute(
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
