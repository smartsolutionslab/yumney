import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { provideYumneyIcons } from '@yumney/ui';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { ActivityComponent } from './activity.component';
import { ActivityApiService, type UserActivityPage } from '../api';

const en = {
  account: {
    activity: {
      title: 'Activity',
      lead: 'Everything…',
      loading: 'Loading…',
      retry: 'Try again',
      loadMore: 'Load older',
      filterAria: 'Filter activity',
      filter: {
        all: 'All',
        imported: 'Imported',
        viewed: 'Viewed',
        cooked: 'Cooked',
        edited: 'Edited',
        deleted: 'Deleted',
      },
    },
  },
};

const firstPage: UserActivityPage = {
  items: Array.from({ length: 20 }, (_, index) => ({
    type: 'recipe_imported',
    recipeIdentifier: `recipe-${index}`,
    recipeTitle: `Recipe ${index}`,
    occurredAt: new Date(2026, 4, 7, 12, 0, index).toISOString(),
  })),
  nextCursor: 'cursor-page-2',
};

const secondPage: UserActivityPage = {
  items: [
    {
      type: 'recipe_imported',
      recipeIdentifier: 'recipe-21',
      recipeTitle: 'Older recipe',
      occurredAt: new Date(2026, 4, 6).toISOString(),
    },
  ],
  nextCursor: null,
};

describe('ActivityComponent', () => {
  let fixture: ComponentFixture<ActivityComponent>;
  let apiMock: { getActivity: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    apiMock = {
      getActivity: vi.fn().mockReturnValue(of(firstPage)),
    };

    await TestBed.configureTestingModule({
      imports: [ActivityComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), { provide: ActivityApiService, useValue: apiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(ActivityComponent);
    fixture.detectChanges();
  });

  it('loads the first page on init with limit=20 and no cursor', () => {
    expect(apiMock.getActivity).toHaveBeenCalledWith({ limit: 20 });
  });

  it('renders the load-more button when nextCursor is present', () => {
    const button = fixture.nativeElement.querySelector('.activity-page__load-more');
    expect(button).toBeTruthy();
    expect(button.textContent).toContain('Load older');
  });

  it('appends entries and forwards the cursor on load-more', () => {
    apiMock.getActivity.mockReturnValueOnce(of(secondPage));

    fixture.componentInstance['onLoadMore']();

    expect(apiMock.getActivity).toHaveBeenCalledWith({ limit: 20, cursor: 'cursor-page-2' });
    expect(fixture.componentInstance['entries']().length).toBe(21);
  });

  it('hides the load-more button after the final page exhausts the cursor', () => {
    apiMock.getActivity.mockReturnValueOnce(of(secondPage));

    fixture.componentInstance['onLoadMore']();
    fixture.detectChanges();

    expect(fixture.componentInstance['nextCursor']()).toBeNull();
    expect(fixture.nativeElement.querySelector('.activity-page__load-more')).toBeFalsy();
  });

  it('resets pagination when filter changes', () => {
    apiMock.getActivity.mockClear();
    apiMock.getActivity.mockReturnValueOnce(of(firstPage));

    fixture.componentInstance['onFilterChange']('recipe_cooked');

    expect(apiMock.getActivity).toHaveBeenCalledWith({ limit: 20, type: 'recipe_cooked' });
  });
});
