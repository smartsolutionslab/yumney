import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import {
  ActivityTimelineComponent,
  AsyncStateComponent,
  provideYumneyIcons,
} from '@yumney/ui';
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

const cookedPage: UserActivityPage = {
  items: [
    {
      type: 'recipe_cooked',
      recipeIdentifier: 'recipe-cooked-1',
      recipeTitle: 'Cooked recipe',
      occurredAt: new Date(2026, 4, 8).toISOString(),
    },
  ],
  nextCursor: null,
};

// Filter chip order matches FILTER_OPTIONS in activity.component.ts.
const ChipIndex = {
  all: 0,
  imported: 1,
  viewed: 2,
  cooked: 3,
  edited: 4,
  deleted: 5,
} as const;

describe('ActivityComponent', () => {
  let fixture: ComponentFixture<ActivityComponent>;
  let apiMock: { getActivity: ReturnType<typeof vi.fn> };

  function chips() {
    return fixture.debugElement.queryAll(By.css('.activity-page__chip'));
  }

  function clickChip(index: number) {
    chips()[index].nativeElement.click();
    fixture.detectChanges();
  }

  function loadMoreButton(): HTMLButtonElement | null {
    return fixture.nativeElement.querySelector('.activity-page__load-more');
  }

  function clickLoadMore() {
    const button = loadMoreButton();
    if (button === null) throw new Error('load-more button not rendered');
    button.click();
    fixture.detectChanges();
  }

  function timelineEntries() {
    const debugEl = fixture.debugElement.query(By.directive(ActivityTimelineComponent));
    return (debugEl.componentInstance as ActivityTimelineComponent).entries();
  }

  function emitRetry() {
    const debugEl = fixture.debugElement.query(By.directive(AsyncStateComponent));
    (debugEl.componentInstance as AsyncStateComponent).retry.emit();
    fixture.detectChanges();
  }

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

  describe('initial load', () => {
    it('should call the API with limit=20 and no cursor on init', () => {
      expect(apiMock.getActivity).toHaveBeenCalledWith({ limit: 20 });
    });

    it('should pass the first page to the timeline', () => {
      expect(timelineEntries()).toHaveLength(20);
    });

    it('should mark the all-filter chip as active by default', () => {
      expect(chips()[ChipIndex.all].nativeElement.getAttribute('aria-pressed')).toBe('true');
    });
  });

  describe('load more', () => {
    it('should render the load-more button when next cursor is present', () => {
      expect(loadMoreButton()).not.toBeNull();
    });

    it('should pass the current cursor to the API when clicked', () => {
      apiMock.getActivity.mockReturnValueOnce(of(secondPage));

      clickLoadMore();

      expect(apiMock.getActivity).toHaveBeenLastCalledWith({
        limit: 20,
        cursor: 'cursor-page-2',
      });
    });

    it('should append the next page to the timeline', () => {
      apiMock.getActivity.mockReturnValueOnce(of(secondPage));

      clickLoadMore();

      expect(timelineEntries()).toHaveLength(21);
    });

    it('should hide the load-more button after the final page exhausts the cursor', () => {
      apiMock.getActivity.mockReturnValueOnce(of(secondPage));

      clickLoadMore();

      expect(loadMoreButton()).toBeNull();
    });
  });

  describe('filter chips', () => {
    it('should call the API with the selected type when a non-active chip is clicked', () => {
      apiMock.getActivity.mockClear();
      apiMock.getActivity.mockReturnValueOnce(of(cookedPage));

      clickChip(ChipIndex.cooked);

      expect(apiMock.getActivity).toHaveBeenCalledWith({ limit: 20, type: 'recipe_cooked' });
    });

    it('should reset the timeline to the new first page after switching filters', () => {
      apiMock.getActivity.mockReturnValueOnce(of(cookedPage));

      clickChip(ChipIndex.cooked);

      expect(timelineEntries()).toHaveLength(1);
    });

    it('should reset the cursor (and hide load-more) after switching to a single-page filter', () => {
      apiMock.getActivity.mockReturnValueOnce(of(cookedPage));

      clickChip(ChipIndex.cooked);

      expect(loadMoreButton()).toBeNull();
    });

    it('should move the active aria-pressed state when filter changes', () => {
      apiMock.getActivity.mockReturnValueOnce(of(cookedPage));

      clickChip(ChipIndex.cooked);

      expect(chips()[ChipIndex.all].nativeElement.getAttribute('aria-pressed')).toBe('false');
      expect(chips()[ChipIndex.cooked].nativeElement.getAttribute('aria-pressed')).toBe('true');
    });

    it('should not refetch when the active chip is clicked again', () => {
      apiMock.getActivity.mockClear();

      clickChip(ChipIndex.all);

      expect(apiMock.getActivity).not.toHaveBeenCalled();
    });
  });

  describe('retry', () => {
    it('should reload the first page with the active filter when retry is emitted', () => {
      apiMock.getActivity.mockReturnValueOnce(of(cookedPage));
      clickChip(ChipIndex.cooked);
      apiMock.getActivity.mockClear();
      apiMock.getActivity.mockReturnValueOnce(of(cookedPage));

      emitRetry();

      expect(apiMock.getActivity).toHaveBeenCalledWith({ limit: 20, type: 'recipe_cooked' });
    });
  });
});
