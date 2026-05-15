import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { MealPlanApiService, type WeekSuggestion, type WeeklyPlan } from '@yumney/shared/api-client';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { MealSuggestionPanelComponent } from './meal-suggestion-panel.component';

const en = {
  mealPlanner: {
    retry: 'Retry',
    suggest: {
      cta: 'Suggest',
      hint: 'Hint',
      loading: 'Loading',
      reviewTitle: 'Review',
      reviewSubtitle: 'Sub',
      accept: 'Accept',
      accepting: 'Accepting',
      regenerate: 'Regenerate',
      dismiss: 'Dismiss',
    },
  },
};

const sampleSuggestion: WeekSuggestion = {
  week: '2026-W20',
  entries: [
    {
      day: 'Monday',
      mealType: 'Dinner',
      recipeIdentifier: '11111111-1111-1111-1111-111111111111',
      recipeTitle: 'Lasagna',
      freshnessLabel: 'Never cooked',
      reason: 'Highly rated favorite',
    },
    {
      day: 'Tuesday',
      mealType: 'Dinner',
      recipeIdentifier: '22222222-2222-2222-2222-222222222222',
      recipeTitle: 'Salad',
      freshnessLabel: null,
      reason: null,
    },
  ],
};

const emptyPlan: WeeklyPlan = {
  week: '2026-W20',
  isExtendedMode: false,
  slots: [],
};

describe('MealSuggestionPanelComponent', () => {
  let fixture: ComponentFixture<MealSuggestionPanelComponent>;
  let apiMock: {
    suggestWeekPlan: ReturnType<typeof vi.fn>;
    assignRecipe: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    apiMock = {
      suggestWeekPlan: vi.fn().mockReturnValue(of(sampleSuggestion)),
      assignRecipe: vi.fn().mockReturnValue(of(emptyPlan)),
    };

    await TestBed.configureTestingModule({
      imports: [MealSuggestionPanelComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), { provide: MealPlanApiService, useValue: apiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(MealSuggestionPanelComponent);
    fixture.componentRef.setInput('year', 2026);
    fixture.componentRef.setInput('week', 20);
    fixture.detectChanges();
  });

  it('renders the suggest CTA initially', () => {
    expect(fixture.nativeElement.querySelector('[data-testid="meal-suggestion-cta"]')).toBeTruthy();
  });

  it('loads and renders suggestions when CTA is clicked', () => {
    fixture.componentInstance['onSuggest']();
    fixture.detectChanges();

    expect(apiMock.suggestWeekPlan).toHaveBeenCalledWith(2026, 20);
    const result = fixture.nativeElement.querySelector('[data-testid="meal-suggestion-result"]');
    expect(result).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Lasagna');
    expect(fixture.nativeElement.textContent).toContain('Salad');
  });

  it('regenerate re-calls the suggestion API', () => {
    fixture.componentInstance['onSuggest']();
    fixture.componentInstance['onRegenerate']();

    expect(apiMock.suggestWeekPlan).toHaveBeenCalledTimes(2);
  });

  it('accept posts one assignRecipe per entry and emits planAccepted', () => {
    fixture.componentInstance['onSuggest']();
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.planAccepted.subscribe(() => (emitted = true));

    fixture.componentInstance['onAccept']();

    expect(apiMock.assignRecipe).toHaveBeenCalledTimes(2);
    expect(apiMock.assignRecipe).toHaveBeenCalledWith(2026, 20, expect.objectContaining({ day: 'Monday', recipeTitle: 'Lasagna' }));
    expect(emitted).toBe(true);
  });

  it('dismiss clears the suggestion and returns to the CTA', () => {
    fixture.componentInstance['onSuggest']();
    fixture.detectChanges();

    fixture.componentInstance['onDismiss']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="meal-suggestion-cta"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="meal-suggestion-result"]')).toBeNull();
  });

  it('shows the error state when suggestion API fails', () => {
    apiMock.suggestWeekPlan.mockReturnValue(throwError(() => new Error('boom')));
    fixture.componentInstance['onSuggest']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
  });
});
