import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { MealPlanApiService, type MealHistoryEntry } from '@yumney/shared/api-client';
import type { PagedResponse } from '@yumney/shared/models';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { MealHistoryComponent } from './meal-history.component';

const en = {
  mealPlanner: {
    retry: 'Retry',
    history: {
      title: 'History',
      back: 'Back',
      pastWeek: 'Past',
      searchLabel: 'Search',
      searchPlaceholder: 'Search...',
      clearSearch: 'Clear',
      loading: 'Loading...',
      empty: 'No matches.',
    },
  },
};

const sampleResults: PagedResponse<MealHistoryEntry> = {
  items: [
    {
      recipeIdentifier: 'r1',
      recipeTitle: 'Lasagna',
      week: '2024-W12',
      day: 'Wednesday',
      mealType: 'Dinner',
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 50,
};

const emptyResults: PagedResponse<MealHistoryEntry> = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 50,
};

describe('MealHistoryComponent', () => {
  let fixture: ComponentFixture<MealHistoryComponent>;
  let apiMock: { searchHistory: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(async () => {
    apiMock = {
      searchHistory: vi.fn().mockReturnValue(of(sampleResults)),
    };

    await TestBed.configureTestingModule({
      imports: [MealHistoryComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: MealPlanApiService, useValue: apiMock },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(MealHistoryComponent);
    fixture.detectChanges();
  });

  it('loads recent history on init with no term', () => {
    expect(apiMock.searchHistory).toHaveBeenCalledWith(
      expect.objectContaining({ term: undefined, pageSize: 50 }),
    );
  });

  it('renders results', () => {
    const list = fixture.nativeElement.querySelector('[data-testid="meal-history-results"]');
    expect(list).toBeTruthy();
    expect(fixture.nativeElement.textContent).toContain('Lasagna');
  });

  it('shows empty state when no results', () => {
    apiMock.searchHistory.mockReturnValue(of(emptyResults));
    fixture.componentInstance['onRetry']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="meal-history-empty"]')).toBeTruthy();
  });

  it('navigates to the planner for the selected week on entry click', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance['onSelect'](sampleResults.items[0]);

    expect(navigateSpy).toHaveBeenCalledWith(['/meal-planner'], {
      queryParams: { year: 2024, week: 12 },
    });
  });

  it('ignores entries with unparseable week strings', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentInstance['onSelect']({
      ...sampleResults.items[0],
      week: 'garbage',
    });

    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('shows error state on API failure', () => {
    apiMock.searchHistory.mockReturnValue(throwError(() => new Error('boom')));
    fixture.componentInstance['onRetry']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
  });
});
