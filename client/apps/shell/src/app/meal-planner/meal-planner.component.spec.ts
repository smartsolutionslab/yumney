import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MealPlannerComponent } from './meal-planner.component';
import { MealPlanApiService, type WeeklyPlan } from '@yumney/shared/api-client';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';

const en = {
  mealPlanner: {
    previousWeek: 'Previous',
    nextWeek: 'Next',
    loading: 'Loading...',
    servings: 'servings',
    clearSlot: 'Clear',
    retry: 'Retry',
  },
};

function pastPlan(): WeeklyPlan {
  return {
    week: '2024-W01',
    isExtendedMode: false,
    slots: emptyPlan.slots.map((slot, index) =>
      index === 0
        ? {
            ...slot,
            contentType: 'Recipe',
            recipeIdentifier: '11111111-1111-1111-1111-111111111111',
            recipeTitle: 'Lasagna',
            state: 'Cooked',
            isEmpty: false,
          }
        : slot,
    ),
  };
}

const emptyPlan: WeeklyPlan = {
  week: '2026-W15',
  isExtendedMode: false,
  slots: [
    {
      day: 'Monday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
    {
      day: 'Tuesday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
    {
      day: 'Wednesday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
    {
      day: 'Thursday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
    {
      day: 'Friday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
    {
      day: 'Saturday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
    {
      day: 'Sunday',
      mealType: 'Dinner',
      contentType: 'Empty',
      state: 'Planned',
      recipeIdentifier: null,
      recipeTitle: null,
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: true,
    },
  ],
};

describe('MealPlannerComponent', () => {
  let fixture: ComponentFixture<MealPlannerComponent>;
  let apiMock: {
    getWeeklyPlan: ReturnType<typeof vi.fn>;
    clearSlot: ReturnType<typeof vi.fn>;
    copyPlanToWeek: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    apiMock = {
      getWeeklyPlan: vi.fn().mockReturnValue(of(emptyPlan)),
      clearSlot: vi.fn().mockReturnValue(of(emptyPlan)),
      copyPlanToWeek: vi.fn().mockReturnValue(of(emptyPlan)),
    };

    await TestBed.configureTestingModule({
      imports: [MealPlannerComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), provideRouter([]), { provide: MealPlanApiService, useValue: apiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(MealPlannerComponent);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render 7 day cards', () => {
    const cards = fixture.nativeElement.querySelectorAll('.day-card');
    expect(cards.length).toBe(7);
  });

  it('should show week label', () => {
    const h1 = fixture.nativeElement.querySelector('h1');
    expect(h1.textContent).toContain('W');
  });

  it('should call API on init', () => {
    expect(apiMock.getWeeklyPlan).toHaveBeenCalled();
  });

  it('should show empty slots with plus icon', () => {
    const emptySlots = fixture.nativeElement.querySelectorAll('.empty-slot');
    expect(emptySlots.length).toBe(7);
  });

  it('should show recipe when slot has content', async () => {
    const planWithRecipe = {
      ...emptyPlan,
      slots: emptyPlan.slots.map((s) => (s.day === 'Monday' ? { ...s, contentType: 'Recipe', recipeTitle: 'Pasta', isEmpty: false } : s)),
    };
    apiMock.getWeeklyPlan.mockReturnValue(of(planWithRecipe));

    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Pasta');
  });

  it('should navigate to next week', () => {
    const initialCalls = apiMock.getWeeklyPlan.mock.calls.length;

    fixture.componentInstance['onNextWeek']();
    fixture.detectChanges();

    expect(apiMock.getWeeklyPlan.mock.calls.length).toBeGreaterThan(initialCalls);
  });

  it('should show error with retry on failure', async () => {
    apiMock.getWeeklyPlan.mockReturnValue(throwError(() => new Error('fail')));

    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.retry-btn')).toBeTruthy();
  });

  it('should navigate to previous week', () => {
    const initialCalls = apiMock.getWeeklyPlan.mock.calls.length;

    fixture.componentInstance['onPreviousWeek']();
    fixture.detectChanges();

    expect(apiMock.getWeeklyPlan.mock.calls.length).toBeGreaterThan(initialCalls);
  });

  it('should call clearSlot API when clearing a slot', () => {
    fixture.componentInstance['onClearSlot']('Monday');

    expect(apiMock.clearSlot).toHaveBeenCalledWith(expect.any(Number), expect.any(Number), {
      day: 'Monday',
    });
  });

  it('should navigate to next week on left swipe (touch)', () => {
    const initialCalls = apiMock.getWeeklyPlan.mock.calls.length;
    const host: HTMLElement = fixture.nativeElement;

    host.dispatchEvent(new PointerEvent('pointerdown', { pointerType: 'touch', clientX: 300, clientY: 100 }));
    host.dispatchEvent(new PointerEvent('pointerup', { pointerType: 'touch', clientX: 200, clientY: 105 }));

    expect(apiMock.getWeeklyPlan.mock.calls.length).toBeGreaterThan(initialCalls);
  });

  it('should navigate to previous week on right swipe (touch)', () => {
    const initialCalls = apiMock.getWeeklyPlan.mock.calls.length;
    const host: HTMLElement = fixture.nativeElement;

    host.dispatchEvent(new PointerEvent('pointerdown', { pointerType: 'touch', clientX: 100, clientY: 100 }));
    host.dispatchEvent(new PointerEvent('pointerup', { pointerType: 'touch', clientX: 220, clientY: 95 }));

    expect(apiMock.getWeeklyPlan.mock.calls.length).toBeGreaterThan(initialCalls);
  });

  it('should ignore mouse drags — swipe is touch-only', () => {
    const initialCalls = apiMock.getWeeklyPlan.mock.calls.length;
    const host: HTMLElement = fixture.nativeElement;

    host.dispatchEvent(new PointerEvent('pointerdown', { pointerType: 'mouse', clientX: 300, clientY: 100 }));
    host.dispatchEvent(new PointerEvent('pointerup', { pointerType: 'mouse', clientX: 150, clientY: 100 }));

    expect(apiMock.getWeeklyPlan.mock.calls.length).toBe(initialCalls);
  });

  it('should ignore mostly-vertical drags (treat as scroll, not swipe)', () => {
    const initialCalls = apiMock.getWeeklyPlan.mock.calls.length;
    const host: HTMLElement = fixture.nativeElement;

    host.dispatchEvent(new PointerEvent('pointerdown', { pointerType: 'touch', clientX: 200, clientY: 100 }));
    host.dispatchEvent(new PointerEvent('pointerup', { pointerType: 'touch', clientX: 260, clientY: 300 }));

    expect(apiMock.getWeeklyPlan.mock.calls.length).toBe(initialCalls);
  });

  it('should hide the clear button on past-week slots (read-only)', () => {
    const past = pastPlan();
    apiMock.getWeeklyPlan.mockReturnValue(of(past));
    fixture.componentInstance['year'].set(2024);
    fixture.componentInstance['weekNumber'].set(1);
    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.clear-btn')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="meal-planner-past-badge"]')).toBeTruthy();
  });

  it('should show the copy-to-current button on past weeks with recipes', () => {
    const past = pastPlan();
    apiMock.getWeeklyPlan.mockReturnValue(of(past));
    fixture.componentInstance['year'].set(2024);
    fixture.componentInstance['weekNumber'].set(1);
    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    const copyBtn = fixture.nativeElement.querySelector('[data-testid="meal-planner-copy-to-current"]');
    expect(copyBtn).toBeTruthy();
  });

  it('should call copyPlanToWeek when copy button is clicked', () => {
    const past = pastPlan();
    apiMock.getWeeklyPlan.mockReturnValue(of(past));
    fixture.componentInstance['year'].set(2024);
    fixture.componentInstance['weekNumber'].set(1);
    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    fixture.componentInstance['onCopyToCurrentWeek']();

    expect(apiMock.copyPlanToWeek).toHaveBeenCalledWith(2024, 1, expect.any(Number), expect.any(Number));
  });

  it('should hide the generate-shopping-list button on past weeks', () => {
    const past = pastPlan();
    apiMock.getWeeklyPlan.mockReturnValue(of(past));
    fixture.componentInstance['year'].set(2024);
    fixture.componentInstance['weekNumber'].set(1);
    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.generate-btn')).toBeNull();
  });

  it('should render a clickable clear button for populated slots', () => {
    const planWithRecipe = {
      ...emptyPlan,
      slots: emptyPlan.slots.map((s) => (s.day === 'Monday' ? { ...s, contentType: 'Recipe', recipeTitle: 'Pasta', isEmpty: false } : s)),
    };
    apiMock.getWeeklyPlan.mockReturnValue(of(planWithRecipe));
    fixture.componentInstance['loadPlan']();
    fixture.detectChanges();

    const clearBtn: HTMLButtonElement | null = fixture.nativeElement.querySelector('.clear-btn');
    expect(clearBtn).toBeTruthy();
    clearBtn!.click();

    expect(apiMock.clearSlot).toHaveBeenCalledWith(expect.any(Number), expect.any(Number), {
      day: 'Monday',
    });
  });
});
