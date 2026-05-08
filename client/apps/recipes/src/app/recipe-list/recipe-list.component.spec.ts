import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { RecipeListComponent } from './recipe-list.component';
import {
  RecipeApiService,
  RecipeListResponse,
  ShoppingApiService,
  ShoppingListDetail,
} from '../api';
import { setupTranslocoTesting } from '@yumney/shared/models';
import { Router } from '@angular/router';

vi.mock('@yumney/ui', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@yumney/ui')>();
  return {
    ...actual,
    staggerFadeIn: vi.fn(),
    prefersReducedMotion: vi.fn(() => true),
  };
});

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
      tags: [],
      isFavorite: false,
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
      tags: [],
      isFavorite: false,
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
      search: {
        placeholder: 'Search recipes...',
        label: 'Search recipes',
        noResults: 'No recipes found',
        noResultsMessage: 'No recipes match "{{query}}". Try a different search term.',
      },
      empty: {
        title: 'No recipes yet',
        message: 'Import your first recipe from any website or create one from scratch.',
        cta: 'Import a Recipe',
      },
      errors: {
        generic: 'Failed to load recipes. Please try again later.',
      },
      multiSelect: {
        enter: 'Select multiple',
        exit: 'Cancel selection',
        fab: 'Create shopping list ({{count}} recipes)',
        creating: 'Creating...',
        autoTitle: 'Meal Prep ({{count}} recipes)',
        selectAriaLabel: 'Select recipe',
        deselectAriaLabel: 'Deselect recipe',
      },
    },
  },
};

describe('RecipeListComponent', () => {
  let component: RecipeListComponent;
  let fixture: ComponentFixture<RecipeListComponent>;
  let recipeApiMock: {
    getRecipes: ReturnType<typeof vi.fn>;
    getRecipeById: ReturnType<typeof vi.fn>;
    toggleFavorite: ReturnType<typeof vi.fn>;
  };
  let shoppingApiMock: {
    createShoppingListFromRecipes: ReturnType<typeof vi.fn>;
  };

  beforeAll(() => {
    vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);
  });

  afterAll(() => {
    vi.unstubAllGlobals();
  });

  function setupTestBed(getRecipesReturn: ReturnType<typeof vi.fn> = vi.fn()) {
    recipeApiMock = {
      getRecipes: getRecipesReturn,
      getRecipeById: vi.fn(),
      toggleFavorite: vi.fn(),
    };
    shoppingApiMock = {
      createShoppingListFromRecipes: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [RecipeListComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: ShoppingApiService, useValue: shoppingApiMock },
      ],
    });

    fixture = TestBed.createComponent(RecipeListComponent);
    component = fixture.componentInstance;
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

    const cards = fixture.nativeElement.querySelectorAll('yn-recipe-card');
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

    const cta = fixture.nativeElement.querySelector('.empty-state a.btn-primary');
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

    const skeleton = fixture.nativeElement.querySelector('.skeleton-grid');
    expect(skeleton).toBeTruthy();

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
    component.onSortSelect('name-asc');
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
    component.onSortSelect('date-asc');
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
          tags: [],
          isFavorite: false,
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
    component.onSortSelect('name-asc');

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
    component.onSortSelect('date-desc');
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

    const dropdown = fixture.nativeElement.querySelector('.sort-dropdown');
    expect(dropdown).toBeTruthy();

    const toggle = dropdown.querySelector('.sort-toggle') as HTMLButtonElement;
    expect(toggle).toBeTruthy();

    toggle.click();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.sort-menu-item');
    expect(items.length).toBe(6);
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

  it('should render search input', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('.search-input');
    expect(input).toBeTruthy();
    expect(input.type).toBe('search');
  }));

  it('should debounce search input and call API after 300ms', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipes.mockClear();
    recipeApiMock.getRecipes.mockReturnValue(of(emptyResponse));

    component.onSearchInput({ target: { value: 'pasta' } } as unknown as Event);
    expect(recipeApiMock.getRecipes).not.toHaveBeenCalled();

    tick(300);
    expect(recipeApiMock.getRecipes).toHaveBeenCalledWith(
      expect.objectContaining({ search: 'pasta' }),
    );
  }));

  it('should reset pagination when searching', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.currentPage.set(3);
    recipeApiMock.getRecipes.mockReturnValue(of(emptyResponse));
    component.onSearchInput({ target: { value: 'test' } } as unknown as Event);
    tick(300);

    expect(component.currentPage()).toBe(1);
  }));

  it('should show no-results empty state when search has no results', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipes.mockReturnValue(of(emptyResponse));
    component.onSearchInput({ target: { value: 'nonexistent' } } as unknown as Event);
    tick(300);
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('.empty-state');
    expect(empty).toBeTruthy();
    expect(empty.textContent).toContain('No recipes found');
  }));

  it('should clear search and reload recipes', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();

    component.searchQuery.set('pasta');
    component.activeSearch.set('pasta');

    recipeApiMock.getRecipes.mockClear();
    recipeApiMock.getRecipes.mockReturnValue(of(mockResponse));
    component.onSearchClear();
    tick();

    expect(component.searchQuery()).toBe('');
    expect(component.activeSearch()).toBe('');
    expect(recipeApiMock.getRecipes).toHaveBeenCalledWith(
      expect.not.objectContaining({ search: expect.anything() }),
    );
  }));

  it('should not include search param when search is empty', fakeAsync(() => {
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

  it('should optimistically flip isFavorite when toggled', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    recipeApiMock.toggleFavorite.mockReturnValue(
      new Subject<{ recipeIdentifier: string; isFavorite: boolean }>(),
    );

    component.onToggleFavorite('abc-123');

    const recipe = component.recipes().find((r) => r.identifier === 'abc-123');
    expect(recipe?.isFavorite).toBe(true);
  }));

  it('should sync isFavorite with server response', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    recipeApiMock.toggleFavorite.mockReturnValue(
      of({ recipeIdentifier: 'abc-123', isFavorite: true }),
    );

    component.onToggleFavorite('abc-123');
    tick();

    expect(component.recipes().find((r) => r.identifier === 'abc-123')?.isFavorite).toBe(true);
  }));

  it('should rollback isFavorite when server call errors', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    recipeApiMock.toggleFavorite.mockReturnValue(throwError(() => new Error('boom')));

    component.onToggleFavorite('abc-123');
    tick();

    expect(component.recipes().find((r) => r.identifier === 'abc-123')?.isFavorite).toBe(false);
  }));

  it('should call toggleFavorite api with the recipe identifier', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    recipeApiMock.toggleFavorite.mockReturnValue(
      of({ recipeIdentifier: 'abc-123', isFavorite: true }),
    );

    component.onToggleFavorite('abc-123');
    tick();

    expect(recipeApiMock.toggleFavorite).toHaveBeenCalledWith('abc-123');
  }));

  it('should ignore toggle for unknown identifier', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.onToggleFavorite('not-in-list');

    expect(recipeApiMock.toggleFavorite).not.toHaveBeenCalled();
  }));

  it('should count favoritesOnly toward filterActiveCount', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();

    component.onFilterChange({
      tags: [],
      difficulty: null,
      maxPrepTime: null,
      maxCookTime: null,
      favoritesOnly: true,
    });

    expect(component.filterActiveCount()).toBe(1);
  }));

  it('should pass favorites=true when filter favoritesOnly is set', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(emptyResponse)));
    fixture.detectChanges();
    tick();
    recipeApiMock.getRecipes.mockClear();

    component.onFilterChange({
      tags: [],
      difficulty: null,
      maxPrepTime: null,
      maxCookTime: null,
      favoritesOnly: true,
    });
    tick();

    expect(recipeApiMock.getRecipes).toHaveBeenCalledWith(
      expect.objectContaining({ favorites: true }),
    );
  }));

  it('should not show the FAB until recipes are selected', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    component.multiSelect.toggleMode();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="recipe-list-multi-select-fab"]'),
    ).toBeNull();
  }));

  it('should show the FAB with the selection count once a recipe is selected', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.toggleSelection('def-456');
    fixture.detectChanges();

    const fab = fixture.nativeElement.querySelector('[data-testid="recipe-list-multi-select-fab"]');
    expect(fab).toBeTruthy();
    expect(fab.textContent).toContain('Create shopping list (2 recipes)');
  }));

  it('should toggle selection on/off for the same identifier', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    expect(component.multiSelect.selectedRecipeIds().has('abc-123')).toBe(true);

    component.multiSelect.toggleSelection('abc-123');
    expect(component.multiSelect.selectedRecipeIds().has('abc-123')).toBe(false);
  }));

  it('should clear selection when leaving multi-select mode', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    expect(component.multiSelect.selectedRecipeIds().size).toBe(1);

    component.multiSelect.toggleMode();

    expect(component.multiSelect.multiSelectMode()).toBe(false);
    expect(component.multiSelect.selectedRecipeIds().size).toBe(0);
  }));

  it('should open the preview dialog and load each selected recipe', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipeById.mockImplementation((identifier: string) =>
      of({
        identifier,
        title: identifier === 'abc-123' ? 'Pasta' : 'Salad',
        servings: identifier === 'abc-123' ? 4 : 2,
        ingredients: [{ name: 'Flour', amount: 200, unit: 'g' }],
      }),
    );

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.toggleSelection('def-456');
    component.multiSelect.openPreview();
    tick();

    expect(component.multiSelect.showPreviewDialog()).toBe(true);
    expect(recipeApiMock.getRecipeById).toHaveBeenCalledTimes(2);
    expect(component.multiSelect.previewSelections()).toHaveLength(2);
    expect(component.multiSelect.previewSelections()[0].desiredServings).toBe(4);
    expect(component.multiSelect.previewSelections()[1].desiredServings).toBe(2);
    expect(shoppingApiMock.createShoppingListFromRecipes).not.toHaveBeenCalled();
  }));

  it('should POST adjusted servings and navigate when the preview is confirmed', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipeById.mockImplementation((identifier: string) =>
      of({
        identifier,
        title: 'Pasta',
        servings: 4,
        ingredients: [{ name: 'Flour', amount: 200, unit: 'g' }],
      }),
    );
    shoppingApiMock.createShoppingListFromRecipes.mockReturnValue(
      of({ identifier: 'list-1' } as unknown as ShoppingListDetail),
    );
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.openPreview();
    tick();
    component.multiSelect.changeServings('abc-123', 6);
    component.multiSelect.confirmPreview();
    tick();

    expect(shoppingApiMock.createShoppingListFromRecipes).toHaveBeenCalledTimes(1);
    const payload = shoppingApiMock.createShoppingListFromRecipes.mock.calls[0][0];
    expect(payload.title).toBe('Meal Prep (1 recipes)');
    expect(payload.recipes).toEqual([{ recipeIdentifier: 'abc-123', servings: 6 }]);
    expect(navigateSpy).toHaveBeenCalledWith('/shopping/lists/list-1');
    expect(component.multiSelect.showPreviewDialog()).toBe(false);
    expect(component.multiSelect.multiSelectMode()).toBe(false);
    navigateSpy.mockRestore();
  }));

  it('should close the preview without posting when cancelled', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipeById.mockReturnValue(
      of({
        identifier: 'abc-123',
        title: 'Pasta',
        servings: 4,
        ingredients: [],
      }),
    );

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.openPreview();
    tick();
    component.multiSelect.cancelPreview();

    expect(component.multiSelect.showPreviewDialog()).toBe(false);
    expect(component.multiSelect.multiSelectMode()).toBe(true);
    expect(component.multiSelect.selectedRecipeIds().has('abc-123')).toBe(true);
    expect(shoppingApiMock.createShoppingListFromRecipes).not.toHaveBeenCalled();
  }));

  it('should not call the API when no recipes are selected', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.multiSelect.toggleMode();
    component.multiSelect.openPreview();
    tick();

    expect(shoppingApiMock.createShoppingListFromRecipes).not.toHaveBeenCalled();
  }));

  it('should surface a server error when the from-recipes API fails', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipeById.mockReturnValue(
      of({
        identifier: 'abc-123',
        title: 'Pasta',
        servings: 4,
        ingredients: [],
      }),
    );
    shoppingApiMock.createShoppingListFromRecipes.mockReturnValue(
      throwError(() => ({ status: 500 })),
    );

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.openPreview();
    tick();
    component.multiSelect.confirmPreview();
    tick();
    fixture.detectChanges();

    expect(component.serverError()).toBeTruthy();
    expect(component.multiSelect.multiSelectMode()).toBe(true);
    expect(component.multiSelect.selectedRecipeIds().has('abc-123')).toBe(true);
  }));

  it('should surface a server error if the recipe detail fetch fails', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getRecipeById.mockReturnValue(throwError(() => ({ status: 500 })));

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.openPreview();
    tick();
    fixture.detectChanges();

    expect(component.multiSelect.showPreviewDialog()).toBe(false);
    expect(component.serverError()).toBeTruthy();
    expect(shoppingApiMock.createShoppingListFromRecipes).not.toHaveBeenCalled();
  }));

  it('should clear the selection when search/sort/filter triggers a reload', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();

    component.multiSelect.toggleMode();
    component.multiSelect.toggleSelection('abc-123');
    component.multiSelect.toggleSelection('def-456');
    expect(component.multiSelect.selectedRecipeIds().size).toBe(2);

    component.onSortSelect('name-asc');
    tick();

    expect(component.multiSelect.selectedRecipeIds().size).toBe(0);
    expect(component.multiSelect.multiSelectMode()).toBe(true);
  }));

  it('should hide the multi-select toggle while in assign mode', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockResponse)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="recipe-list-multi-select-toggle"]'),
    ).toBeTruthy();

    component['assignment'].assignTo.set('2026-W18-monday');
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="recipe-list-multi-select-toggle"]'),
    ).toBeNull();
  }));
});
