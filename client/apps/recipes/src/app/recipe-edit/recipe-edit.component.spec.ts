import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { RecipeEditComponent } from './recipe-edit.component';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';
import { HttpErrorResponse } from '@angular/common/http';
import { setupTranslocoTesting } from '@yumney/shared/models';

const mockRecipeDetail: RecipeDetail = {
  identifier: 'abc-123',
  title: 'Pasta Carbonara',
  description: 'A classic Italian dish',
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: 'https://example.com/image.jpg',
  sourceUrl: 'https://example.com/recipe',
  createdAt: '2026-03-10T00:00:00Z',
  ingredients: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
  ],
  steps: [
    { number: 1, description: 'Cook pasta' },
    { number: 2, description: 'Mix eggs' },
  ],
};

const en = {
  shared: {
    editableList: {
      moveUp: 'Move up',
      moveDown: 'Move down',
      remove: 'Remove',
    },
  },
  recipes: {
    edit: {
      title: 'Edit Recipe',
      back: 'Back to Recipe',
      loading: 'Loading recipe...',
      success: 'Recipe "{{title}}" updated successfully!',
      errors: {
        notFound: 'Recipe not found.',
        generic: 'Failed to update recipe. Please try again later.',
      },
    },
  },
  dashboard: {
    preview: {
      title: 'Review Extracted Recipe',
      recipeTitle: 'Title',
      description: 'Description',
      servings: 'Servings',
      prepTime: 'Prep Time (min)',
      cookTime: 'Cook Time (min)',
      difficulty: 'Difficulty',
      ingredients: 'Ingredients',
      ingredientName: 'Ingredient',
      amount: 'Amount',
      unit: 'Unit',
      addIngredient: 'Add Ingredient',
      steps: 'Steps',
      stepDescription: 'Step Description',
      addStep: 'Add Step',
      save: 'Save Recipe',
      discard: 'Discard',
      errors: {
        titleRequired: 'Title is required.',
        titleMaxLength: 'Title must not exceed 200 characters.',
        ingredientNameRequired: 'Ingredient name is required.',
        stepDescriptionRequired: 'Step description is required.',
      },
    },
    save: {
      saving: 'Saving...',
    },
  },
};

describe('RecipeEditComponent', () => {
  let component: RecipeEditComponent;
  let fixture: ComponentFixture<RecipeEditComponent>;
  let recipeApiMock: {
    getRecipeById: ReturnType<typeof vi.fn>;
    updateRecipe: ReturnType<typeof vi.fn>;
  };
  let router: Router;

  function setupTestBed(
    getRecipeByIdReturn: ReturnType<typeof vi.fn> = vi.fn(),
    updateRecipeReturn: ReturnType<typeof vi.fn> = vi.fn(),
    identifier = 'abc-123',
  ) {
    recipeApiMock = {
      getRecipeById: getRecipeByIdReturn,
      updateRecipe: updateRecipeReturn,
    };

    TestBed.configureTestingModule({
      imports: [RecipeEditComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: RecipeApiService, useValue: recipeApiMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'identifier' ? identifier : null),
              },
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(RecipeEditComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  }

  it('should create the component', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();

    expect(component).toBeTruthy();
  }));

  it('should load recipe on init', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();

    expect(recipeApiMock.getRecipeById).toHaveBeenCalledWith('abc-123');
    expect(component.recipeData()).toBeTruthy();
    expect(component.recipeData()?.title).toBe('Pasta Carbonara');
  }));

  it('should show loading state while fetching', fakeAsync(() => {
    const subject = new Subject<RecipeDetail>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    const loading = fixture.nativeElement.querySelector('.loading');
    expect(loading).toBeTruthy();
    expect(loading.textContent).toContain('Loading recipe...');

    subject.next(mockRecipeDetail);
    subject.complete();
    tick();
  }));

  it('should hide loading state after fetch', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();

    expect(component.isLoading()).toBe(false);
  }));

  it('should show error on fetch failure', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 500 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to update recipe.');
  }));

  it('should show not-found on 404 fetch', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 404 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Recipe not found.');
  }));

  it('should show not-found when route has no identifier', fakeAsync(() => {
    setupTestBed(vi.fn(), vi.fn(), null as unknown as string);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(recipeApiMock.getRecipeById).not.toHaveBeenCalled();
    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Recipe not found.');
  }));

  it('should render recipe-preview after load', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const preview = fixture.nativeElement.querySelector('yn-recipe-preview');
    expect(preview).toBeTruthy();
  }));

  it('should map RecipeDetail to ImportRecipeResponse excluding sourceUrl and createdAt', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();

    const data = component.recipeData();
    expect(data).toBeTruthy();
    expect(data?.title).toBe('Pasta Carbonara');
    expect(data?.description).toBe('A classic Italian dish');
    expect(data?.servings).toBe(4);
    expect(data?.prepTimeMinutes).toBe(10);
    expect(data?.cookTimeMinutes).toBe(20);
    expect(data?.difficulty).toBe('medium');
    expect(data?.imageUrl).toBe('https://example.com/image.jpg');
    expect(data?.ingredients).toEqual(mockRecipeDetail.ingredients);
    expect(data?.steps).toEqual(mockRecipeDetail.steps);
    expect((data as Record<string, unknown>)['sourceUrl']).toBeUndefined();
    expect((data as Record<string, unknown>)['createdAt']).toBeUndefined();
  }));

  it('should navigate to detail on discard', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    component.onDiscard();

    expect(router.navigate).toHaveBeenCalledWith(['/recipes', 'abc-123']);
  }));

  it('should call updateRecipe on save', fakeAsync(() => {
    setupTestBed(
      vi.fn().mockReturnValue(of(mockRecipeDetail)),
      vi.fn().mockReturnValue(of(mockRecipeDetail)),
    );
    fixture.detectChanges();
    tick();

    component.onSave({
      title: 'Updated Pasta',
      description: null,
      ingredients: [{ name: 'Flour', amount: 500, unit: 'g' }],
      steps: [{ number: 1, description: 'Mix' }],
      servings: 2,
      prepTimeMinutes: 5,
      cookTimeMinutes: 10,
      difficulty: 'easy',
      imageUrl: null,
    });
    tick();

    expect(recipeApiMock.updateRecipe).toHaveBeenCalledWith('abc-123', {
      title: 'Updated Pasta',
      description: null,
      ingredients: [{ name: 'Flour', amount: 500, unit: 'g' }],
      steps: [{ number: 1, description: 'Mix' }],
      servings: 2,
      prepTimeMinutes: 5,
      cookTimeMinutes: 10,
      difficulty: 'easy',
      imageUrl: null,
    });
  }));

  it('should navigate to detail on successful update', fakeAsync(() => {
    setupTestBed(
      vi.fn().mockReturnValue(of(mockRecipeDetail)),
      vi.fn().mockReturnValue(of(mockRecipeDetail)),
    );
    fixture.detectChanges();
    tick();

    component.onSave({
      title: 'Updated Pasta',
      description: null,
      ingredients: [{ name: 'Flour', amount: 500, unit: 'g' }],
      steps: [{ number: 1, description: 'Mix' }],
      servings: null,
      prepTimeMinutes: null,
      cookTimeMinutes: null,
      difficulty: null,
      imageUrl: null,
    });
    tick();

    expect(router.navigate).toHaveBeenCalledWith(['/recipes', 'abc-123']);
  }));

  it('should show error on update failure', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 500 });
    setupTestBed(
      vi.fn().mockReturnValue(of(mockRecipeDetail)),
      vi.fn().mockReturnValue(throwError(() => httpError)),
    );
    fixture.detectChanges();
    tick();

    component.onSave({
      title: 'Updated Pasta',
      description: null,
      ingredients: [{ name: 'Flour', amount: 500, unit: 'g' }],
      steps: [{ number: 1, description: 'Mix' }],
      servings: null,
      prepTimeMinutes: null,
      cookTimeMinutes: null,
      difficulty: null,
      imageUrl: null,
    });
    tick();
    fixture.detectChanges();

    expect(component.isSaving()).toBe(false);
    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
  }));

  it('should render back link', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipeDetail)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const backLink = fixture.nativeElement.querySelector('.back-link');
    expect(backLink).toBeTruthy();
    expect(backLink.textContent).toContain('Back to Recipe');
  }));
});
