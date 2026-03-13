import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of, Subject, throwError } from 'rxjs';
import { RecipeListComponent } from './recipe-list.component';
import { RecipeApiService, RecipeListResponse } from '@yumney/shared/api-client';

let intersectionCallback: IntersectionObserverCallback;

class MockIntersectionObserver implements IntersectionObserver {
  readonly root: Element | Document | null = null;
  readonly rootMargin: string = '';
  readonly thresholds: ReadonlyArray<number> = [];
  observedElement: Element | null = null;

  constructor(callback: IntersectionObserverCallback) {
    intersectionCallback = callback;
  }

  observe(target: Element): void {
    this.observedElement = target;
  }

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  unobserve(): void {}

  disconnect(): void {
    this.observedElement = null;
  }

  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }
}

const mockResponse: RecipeListResponse = {
  items: [
    {
      identifier: 'abc-123',
      title: 'Pasta Carbonara',
      description: 'A classic Italian dish',
      servings: 4,
      prepTimeMinutes: 10,
      cookTimeMinutes: 20,
      difficulty: 'medium',
      imageUrl: 'https://example.com/image.jpg',
      createdAt: '2026-03-10T00:00:00Z',
    },
    {
      identifier: 'def-456',
      title: 'Caesar Salad',
      description: null,
      servings: 2,
      prepTimeMinutes: 15,
      cookTimeMinutes: null,
      difficulty: 'easy',
      imageUrl: null,
      createdAt: '2026-03-09T00:00:00Z',
    },
  ],
  totalCount: 5,
  page: 1,
  pageSize: 20,
};

const emptyResponse: RecipeListResponse = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
};

const en = {
  recipes: {
    list: {
      title: 'My Recipes',
      sortLabel: 'Sort recipes',
      sort: {
        dateDesc: 'Newest first',
        dateAsc: 'Oldest first',
        nameAsc: 'Name A-Z',
        nameDesc: 'Name Z-A',
      },
      servings: '{{count}} servings',
      prepTime: 'Prep {{minutes}} min',
      cookTime: 'Cook {{minutes}} min',
      loading: 'Loading recipes...',
      empty: {
        title: 'No recipes yet',
        message: 'Import your first recipe from any website or create one from scratch.',
        cta: 'Import a Recipe',
      },
      errors: {
        generic: 'Failed to load recipes. Please try again later.',
      },
    },
  },
};

describe('RecipeListComponent', () => {
  let component: RecipeListComponent;
  let fixture: ComponentFixture<RecipeListComponent>;
  let recipeApiMock: { getRecipes: ReturnType<typeof vi.fn> };

  beforeAll(() => {
    vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);
  });

  afterAll(() => {
    vi.unstubAllGlobals();
  });

  function setupTestBed(getRecipesReturn: ReturnType<typeof vi.fn> = vi.fn()) {
    recipeApiMock = {
      getRecipes: getRecipesReturn,
    };

    TestBed.configureTestingModule({
      imports: [
        RecipeListComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [provideRouter([]), { provide: RecipeApiService, useValue: recipeApiMock }],
    });

    fixture = TestBed.createComponent(RecipeListComponent);
    component = fixture.componentInstance;
  }

  function mockSortEvent(value: string): Event {
    return { target: { value } } as unknown as Event;
  }

  function triggerIntersection(isIntersecting: boolean): void {
    intersectionCallback(
      [{ isIntersecting } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );
  }

  it('should create the component', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();

    expect(component).toBeTruthy();
  }));

  it('should render recipe cards', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('.recipe-card');
    expect(cards.length).toBe(2);
  }));

  it('should display recipe title in card', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const titles = fixture.nativeElement.querySelectorAll('.recipe-title');
    expect(titles[0].textContent).toContain('Pasta Carbonara');
  }));

  it('should show empty state when no recipes', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-state');
    expect(empty).toBeTruthy();
    expect(empty.textContent).toContain('No recipes yet');
  }));

  it('should show CTA link to dashboard in empty state', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const cta = fixture.nativeElement.querySelector('.cta-button');
    expect(cta).toBeTruthy();
    expect(cta.textContent).toContain('Import a Recipe');
  }));

  it('should not show empty state when recipes exist', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-state');
    expect(empty).toBeNull();
  }));

  it('should call getRecipes on init', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();

    expect(recipeApiMock.getRecipes).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      sortBy: 'Date',
      sortDirection: 'Descending',
    });
  }));

  it('should show loading state', fakeAsync(() => {
    const subject = new Subject<RecipeListResponse>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    const loading = fixture.nativeElement.querySelector('.loading');
    expect(loading).toBeTruthy();
    expect(loading.textContent).toContain('Loading recipes...');

    subject.next(emptyResponse);
    subject.complete();
    tick();
  }));

  it('should show error state on API failure', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(throwError(() => new Error('fail'))));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to load recipes.');
  }));

  it('should reload on sort change', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipes.mockReturnValue(of(mockResponse));
    component.onSortChange(mockSortEvent('name-asc'));
    tick();

    expect(recipeApiMock.getRecipes).toHaveBeenCalledWith(
      expect.objectContaining({ sortBy: 'Name', sortDirection: 'Ascending' }),
    );
  }));

  it('should reset page on sort change', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.currentPage.set(3);
    recipeApiMock.getRecipes.mockReturnValue(of(mockResponse));
    component.onSortChange(mockSortEvent('date-asc'));
    tick();

    expect(component.currentPage()).toBe(1);
  }));

  it('should render scroll sentinel element', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const sentinel = fixture.nativeElement.querySelector('.scroll-sentinel');
    expect(sentinel).toBeTruthy();
  }));

  it('should call loadMore when sentinel intersects and hasMore', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    const moreResponse: RecipeListResponse = {
      items: [
        {
          identifier: 'ghi-789',
          title: 'Tomato Soup',
          description: null,
          servings: 6,
          prepTimeMinutes: 5,
          cookTimeMinutes: 30,
          difficulty: 'easy',
          imageUrl: null,
          createdAt: '2026-03-08T00:00:00Z',
        },
      ],
      totalCount: 5,
      page: 2,
      pageSize: 20,
    };
    recipeApiMock.getRecipes.mockReturnValue(of(moreResponse));
    triggerIntersection(true);
    tick();

    expect(component.recipes().length).toBe(3);
    expect(component.currentPage()).toBe(2);
  }));

  it('should not call loadMore when sentinel intersects but no more items', fakeAsync(() => {
    const fullResponse: RecipeListResponse = { ...mockResponse, totalCount: 2 };
    setupTestBed(vi.fn().mockReturnValue(of(fullResponse)));
    fixture.detectChanges();
    tick();

    const callCount = recipeApiMock.getRecipes.mock.calls.length;
    triggerIntersection(true);
    tick();

    expect(recipeApiMock.getRecipes.mock.calls.length).toBe(callCount);
  }));

  it('should not call loadMore when sentinel is not intersecting', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    const callCount = recipeApiMock.getRecipes.mock.calls.length;
    triggerIntersection(false);
    tick();

    expect(recipeApiMock.getRecipes.mock.calls.length).toBe(callCount);
  }));

  it('should not call loadMore when sentinel intersects but isLoading', fakeAsync(() => {
    const subject = new Subject<RecipeListResponse>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);
    triggerIntersection(true);

    expect(recipeApiMock.getRecipes).toHaveBeenCalledTimes(1);

    subject.next(mockResponse);
    subject.complete();
    tick();
  }));

  it('should disconnect observer on destroy', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    const disconnectSpy = vi.spyOn(MockIntersectionObserver.prototype, 'disconnect');
    fixture.destroy();
    expect(disconnectSpy).toHaveBeenCalled();
    disconnectSpy.mockRestore();
  }));

  it('should show recipe image when available', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('.recipe-image');
    expect(img).toBeTruthy();
    expect(img.src).toContain('example.com/image.jpg');
  }));

  it('should show placeholder when no image', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const placeholder = fixture.nativeElement.querySelector('.recipe-image-placeholder');
    expect(placeholder).toBeTruthy();
  }));

  it('should hide loading after data loads', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const loading = fixture.nativeElement.querySelector('.loading');
    expect(loading).toBeNull();
  }));

  it('should reset totalCount on sort change', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    expect(component.totalCount()).toBe(5);

    const subject = new Subject<RecipeListResponse>();
    recipeApiMock.getRecipes.mockReturnValue(subject);
    component.onSortChange(mockSortEvent('name-asc'));

    expect(component.totalCount()).toBe(0);

    subject.next(mockResponse);
    subject.complete();
    tick();
  }));

  it('should clear error on successful retry', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(throwError(() => new Error('fail'))));
    fixture.detectChanges();
    tick();
    expect(component.serverError()).toBe('recipes.list.errors.generic');

    recipeApiMock.getRecipes.mockReturnValue(of(mockResponse));
    component.onSortChange(mockSortEvent('date-desc'));
    tick();

    expect(component.serverError()).toBeNull();
  }));

  it('should not show empty state while loading', fakeAsync(() => {
    const subject = new Subject<RecipeListResponse>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-state');
    expect(empty).toBeNull();

    subject.next(emptyResponse);
    subject.complete();
    tick();
  }));

  it('should render sort dropdown', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const select = fixture.nativeElement.querySelector('.sort-select');
    expect(select).toBeTruthy();
    expect(select.options.length).toBe(4);
  }));

  it('should display recipe description when present', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const descriptions = fixture.nativeElement.querySelectorAll('.recipe-description');
    expect(descriptions.length).toBe(1);
    expect(descriptions[0].textContent).toContain('A classic Italian dish');
  }));

  it('should set isLoading to false after error', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(throwError(() => new Error('fail'))));
    fixture.detectChanges();
    tick();

    expect(component.isLoading()).toBe(false);
  }));
});
