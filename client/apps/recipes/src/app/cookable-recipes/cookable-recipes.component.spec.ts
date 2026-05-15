import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CookableRecipesComponent } from './cookable-recipes.component';
import { RecipeApiService, type CookableRecipeListResponse } from '../api';
import { setupTranslocoTesting } from '@yumney/shared/models';

vi.mock('@yumney/ui', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@yumney/ui')>();
  return {
    ...actual,
    staggerFadeIn: vi.fn(),
    prefersReducedMotion: vi.fn(() => true),
  };
});

class MockIntersectionObserver implements IntersectionObserver {
  readonly root: Element | Document | null = null;
  readonly rootMargin: string = '';
  readonly thresholds: ReadonlyArray<number> = [];

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  constructor(_: IntersectionObserverCallback) {}

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  observe(): void {}

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  unobserve(): void {}

  // eslint-disable-next-line @typescript-eslint/no-empty-function
  disconnect(): void {}

  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }
}

const fullMatch: CookableRecipeListResponse = {
  items: [
    {
      recipeIdentifier: 'abc-123',
      title: 'Pasta Carbonara',
      imageUrl: null,
      servings: 4,
      prepTimeMinutes: 10,
      cookTimeMinutes: 20,
      difficulty: 'easy',
      ingredientCount: 6,
      tier: 'Full',
      missingIngredients: [],
    },
    {
      recipeIdentifier: 'def-456',
      title: 'Stir Fry',
      imageUrl: null,
      servings: 2,
      prepTimeMinutes: 15,
      cookTimeMinutes: 10,
      difficulty: 'easy',
      ingredientCount: 5,
      tier: 'Near',
      missingIngredients: ['onion', 'garlic'],
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
};

const empty: CookableRecipeListResponse = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
};

const en = {
  recipes: {
    cookable: {
      title: 'What can I cook?',
      subtitle: 'Recipes you have the ingredients for.',
      filter: { fullMatchOnly: 'Ready to cook only' },
      tier: { full: 'Ready to cook', near: 'Almost there' },
      missing: 'Need: {{items}}',
      empty: {
        title: 'No matching recipes',
        message: 'Import more recipes.',
        cta: 'Import a recipe',
      },
      errors: { generic: 'Could not load matching recipes.' },
    },
    list: {
      servings: '{{count}} servings',
      prepTime: 'Prep {{minutes}} min',
      cookTime: 'Cook {{minutes}} min',
    },
  },
};

describe('CookableRecipesComponent', () => {
  let component: CookableRecipesComponent;
  let fixture: ComponentFixture<CookableRecipesComponent>;
  let recipeApiMock: { getCookableRecipes: ReturnType<typeof vi.fn> };

  beforeAll(() => {
    vi.stubGlobal('IntersectionObserver', MockIntersectionObserver);
  });

  afterAll(() => {
    vi.unstubAllGlobals();
  });

  function setupTestBed(returnValue: ReturnType<typeof vi.fn>): void {
    recipeApiMock = { getCookableRecipes: returnValue };

    TestBed.configureTestingModule({
      imports: [CookableRecipesComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), provideRouter([]), { provide: RecipeApiService, useValue: recipeApiMock }],
    });

    fixture = TestBed.createComponent(CookableRecipesComponent);
    component = fixture.componentInstance;
  }

  it('should load cookable recipes on init', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullMatch)));
    fixture.detectChanges();
    tick();

    expect(recipeApiMock.getCookableRecipes).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
    });
    expect(component.recipes()).toHaveLength(2);
  }));

  it('should render a tier badge per recipe', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullMatch)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const badges = fixture.nativeElement.querySelectorAll('[data-testid="cookable-tier-badge"]');
    expect(badges.length).toBe(2);
    expect(badges[0].textContent).toContain('Ready to cook');
    expect(badges[1].textContent).toContain('Almost there');
  }));

  it('should show missing ingredients for near matches only', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullMatch)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const missing = fixture.nativeElement.querySelectorAll('[data-testid="cookable-missing"]');
    expect(missing.length).toBe(1);
    expect(missing[0].textContent).toContain('onion, garlic');
  }));

  it('should toggle fullMatchOnly and reload', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(fullMatch)));
    fixture.detectChanges();
    tick();

    recipeApiMock.getCookableRecipes.mockClear();
    recipeApiMock.getCookableRecipes.mockReturnValue(of(empty));
    component.onToggleFullMatchOnly();
    tick();

    expect(component.fullMatchOnly()).toBe(true);
    expect(recipeApiMock.getCookableRecipes).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      fullMatchOnly: true,
    });
  }));

  it('should show empty state when no recipes match', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(empty)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector('.empty-state');
    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No matching recipes');
  }));

  it('should surface error on API failure', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(throwError(() => new Error('boom'))));
    fixture.detectChanges();
    tick();

    expect(component.serverError()).toBe('recipes.cookable.errors.generic');
  }));
});
