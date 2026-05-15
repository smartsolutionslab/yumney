import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MealPlanApiService, type MealAnalytics } from '@yumney/shared/api-client';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { MealAnalyticsComponent } from './meal-analytics.component';

const en = {
  mealPlanner: {
    retry: 'Retry',
    analytics: {
      back: 'Back',
      title: 'Analytics',
      viewToggle: 'View',
      monthly: 'Month',
      yearly: 'Year',
      previousPeriod: 'Prev',
      nextPeriod: 'Next',
      loading: 'Loading',
      totalCooked: 'Cooked',
      uniqueRecipes: 'Unique',
      skipped: 'Skipped',
      mealsPerWeek: 'Per week',
      discoveryRate: 'New',
      discoveryHint: 'Hint',
      categoryDistribution: 'Mix',
      topRecipes: 'Top',
      timesCooked: '{{count}}x',
      empty: 'None',
      categories: { meat: 'Meat', fish: 'Fish', veggie: 'Veg', vegan: 'Vegan', other: 'Other' },
    },
  },
};

function makeAnalytics(overrides: Partial<MealAnalytics> = {}): MealAnalytics {
  return {
    period: '2026-05',
    totalCooked: 12,
    totalSkipped: 2,
    uniqueRecipes: 7,
    mealsPerWeek: 2.8,
    discoveryRate: 3,
    topRecipes: [
      {
        recipeIdentifier: '11111111-1111-1111-1111-111111111111',
        recipeTitle: 'Lasagna',
        cookCount: 4,
      },
    ],
    categoryDistribution: [
      { category: 'meat', count: 8, percentage: 66.7 },
      { category: 'veggie', count: 4, percentage: 33.3 },
    ],
    ...overrides,
  };
}

describe('MealAnalyticsComponent', () => {
  let fixture: ComponentFixture<MealAnalyticsComponent>;
  let apiMock: { getMealAnalytics: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    apiMock = { getMealAnalytics: vi.fn().mockReturnValue(of(makeAnalytics())) };

    await TestBed.configureTestingModule({
      imports: [MealAnalyticsComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), provideRouter([]), { provide: MealPlanApiService, useValue: apiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(MealAnalyticsComponent);
    fixture.detectChanges();
  });

  it('loads analytics with month view on init', () => {
    expect(apiMock.getMealAnalytics).toHaveBeenCalled();
    const [, month] = apiMock.getMealAnalytics.mock.calls[0];
    expect(typeof month).toBe('number');
  });

  it('renders total cooked stat', () => {
    expect(fixture.nativeElement.querySelector('[data-testid="meal-analytics-total-cooked"]').textContent).toContain('12');
  });

  it('renders the category-distribution legend with segments per category', () => {
    const swatches = fixture.nativeElement.querySelectorAll('.legend-swatch');
    expect(swatches.length).toBe(2);
  });

  it('renders the top-recipes list', () => {
    const list = fixture.nativeElement.querySelector('[data-testid="meal-analytics-top-recipes"]');
    expect(list).toBeTruthy();
    expect(list.textContent).toContain('Lasagna');
  });

  it('switches to year view and reloads without a month param', () => {
    apiMock.getMealAnalytics.mockClear();
    fixture.componentInstance['onSetView']('year');

    expect(apiMock.getMealAnalytics).toHaveBeenCalledTimes(1);
    const [, month] = apiMock.getMealAnalytics.mock.calls[0];
    expect(month).toBeUndefined();
  });

  it('next-period in month view advances the month and reloads', () => {
    fixture.componentInstance['month'].set(3);
    apiMock.getMealAnalytics.mockClear();

    fixture.componentInstance['onNextPeriod']();

    expect(fixture.componentInstance['month']()).toBe(4);
    expect(apiMock.getMealAnalytics).toHaveBeenCalled();
  });

  it('next-period wraps from December to January and bumps the year', () => {
    fixture.componentInstance['month'].set(12);
    fixture.componentInstance['year'].set(2026);

    fixture.componentInstance['onNextPeriod']();

    expect(fixture.componentInstance['month']()).toBe(1);
    expect(fixture.componentInstance['year']()).toBe(2027);
  });

  it('shows the empty hint when there are no cooked meals', () => {
    apiMock.getMealAnalytics.mockReturnValue(of(makeAnalytics({ totalCooked: 0, topRecipes: [], categoryDistribution: [] })));
    fixture.componentInstance['onRetry']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="meal-analytics-empty"]')).toBeTruthy();
  });

  it('surfaces errors from the API', () => {
    apiMock.getMealAnalytics.mockReturnValue(throwError(() => new Error('boom')));
    fixture.componentInstance['onRetry']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
  });
});
