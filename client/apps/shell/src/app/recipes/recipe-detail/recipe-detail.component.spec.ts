import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of, Subject, throwError, EMPTY } from 'rxjs';
import { RecipeDetailComponent } from './recipe-detail.component';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';
import { HttpErrorResponse } from '@angular/common/http';

const mockRecipe: RecipeDetail = {
  identifier: 'abc-123',
  title: 'Pasta Carbonara',
  description: 'A classic Italian dish with eggs and bacon',
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
    { name: 'Parmesan', amount: null, unit: null },
  ],
  steps: [
    { number: 1, description: 'Cook pasta' },
    { number: 2, description: 'Mix eggs and cheese' },
    { number: 3, description: 'Combine everything' },
  ],
};

const en = {
  recipes: {
    detail: {
      back: 'Back to Recipes',
      servings: '{{count}} servings',
      prepTime: 'Prep',
      cookTime: 'Cook',
      totalTime: 'Total',
      minutes: '{{minutes}} min',
      difficulty: 'Difficulty',
      ingredients: 'Ingredients',
      steps: 'Steps',
      sourceUrl: 'Original Recipe',
      actions: {
        edit: 'Edit',
        delete: 'Delete',
        shoppingList: 'Shopping List',
      },
      loading: 'Loading recipe...',
      notFound: 'Recipe not found.',
      delete: {
        confirm: 'Are you sure you want to delete "{{title}}"? This cannot be undone.',
        deleting: 'Deleting...',
        success: 'Recipe deleted successfully.',
        errors: {
          notFound: 'Recipe not found.',
          generic: 'Failed to delete recipe. Please try again later.',
        },
      },
      errors: {
        generic: 'Failed to load recipe. Please try again later.',
      },
    },
  },
};

describe('RecipeDetailComponent', () => {
  let component: RecipeDetailComponent;
  let fixture: ComponentFixture<RecipeDetailComponent>;
  let recipeApiMock: {
    getRecipeById: ReturnType<typeof vi.fn>;
    deleteRecipe: ReturnType<typeof vi.fn>;
  };

  function setupTestBed(
    getRecipeByIdReturn: ReturnType<typeof vi.fn> = vi.fn(),
    identifier = 'abc-123',
  ) {
    recipeApiMock = {
      getRecipeById: getRecipeByIdReturn,
      deleteRecipe: vi.fn().mockReturnValue(EMPTY),
    };

    TestBed.configureTestingModule({
      imports: [
        RecipeDetailComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [
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

    fixture = TestBed.createComponent(RecipeDetailComponent);
    component = fixture.componentInstance;
  }

  it('should create the component', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component).toBeTruthy();
  }));

  it('should render recipe title', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const title = fixture.nativeElement.querySelector('.recipe-title');
    expect(title.textContent).toContain('Pasta Carbonara');
  }));

  it('should render recipe description', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const desc = fixture.nativeElement.querySelector('.recipe-description');
    expect(desc.textContent).toContain('A classic Italian dish');
  }));

  it('should render recipe image', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('.hero-image');
    expect(img).toBeTruthy();
    expect(img.src).toContain('example.com/image.jpg');
  }));

  it('should show placeholder when no image', fakeAsync(() => {
    const noImage = { ...mockRecipe, imageUrl: null };
    setupTestBed(vi.fn().mockReturnValue(of(noImage)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const placeholder = fixture.nativeElement.querySelector('.hero-placeholder');
    expect(placeholder).toBeTruthy();
  }));

  it('should render ingredients list', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const items = fixture.nativeElement.querySelectorAll('.ingredients-list li');
    expect(items.length).toBe(3);
  }));

  it('should render ingredient with amount and unit', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const firstItem = fixture.nativeElement.querySelector('.ingredients-list li');
    expect(firstItem.textContent).toContain('400');
    expect(firstItem.textContent).toContain('g');
    expect(firstItem.textContent).toContain('Spaghetti');
  }));

  it('should render steps in order', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const steps = fixture.nativeElement.querySelectorAll('.steps-list li');
    expect(steps.length).toBe(3);
    expect(steps[0].textContent).toContain('Cook pasta');
    expect(steps[1].textContent).toContain('Mix eggs and cheese');
    expect(steps[2].textContent).toContain('Combine everything');
  }));

  it('should show meta bar with servings', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const meta = fixture.nativeElement.querySelector('.meta-bar');
    expect(meta.textContent).toContain('4 servings');
  }));

  it('should hide optional fields when null', fakeAsync(() => {
    const minimal: RecipeDetail = {
      ...mockRecipe,
      description: null,
      servings: null,
      prepTimeMinutes: null,
      cookTimeMinutes: null,
      difficulty: null,
      sourceUrl: null,
    };
    setupTestBed(vi.fn().mockReturnValue(of(minimal)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.recipe-description')).toBeNull();
    expect(fixture.nativeElement.querySelector('.source-link')).toBeNull();
    const metaItems = fixture.nativeElement.querySelectorAll('.meta-item');
    expect(metaItems.length).toBe(0);
  }));

  it('should show loading state', fakeAsync(() => {
    const subject = new Subject<RecipeDetail>();
    setupTestBed(vi.fn().mockReturnValue(subject));
    fixture.detectChanges();

    const loading = fixture.nativeElement.querySelector('.loading');
    expect(loading).toBeTruthy();
    expect(loading.textContent).toContain('Loading recipe...');

    subject.next(mockRecipe);
    subject.complete();
    tick();
  }));

  it('should show error state on API failure', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 500 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to load recipe.');
  }));

  it('should show not-found message on 404', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 404 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Recipe not found.');
  }));

  it('should render back link', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const backLink = fixture.nativeElement.querySelector('.back-link');
    expect(backLink).toBeTruthy();
    expect(backLink.textContent).toContain('Back to Recipes');
  }));

  it('should render source URL as link', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const sourceLink = fixture.nativeElement.querySelector('.source-link a');
    expect(sourceLink).toBeTruthy();
    expect(sourceLink.href).toContain('example.com/recipe');
    expect(sourceLink.textContent).toContain('Original Recipe');
  }));

  it('should render edit button as link with correct routerLink', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const editLink = fixture.nativeElement.querySelector('.action-button[href]');
    expect(editLink).toBeTruthy();
    expect(editLink.tagName).toBe('A');
    expect(editLink.getAttribute('href')).toBe('/recipes/abc-123/edit');
    expect(editLink.textContent.trim()).toBe('Edit');
  }));

  it('should render shopping list button as disabled', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const disabledButtons = fixture.nativeElement.querySelectorAll('.action-button:disabled');
    expect(disabledButtons.length).toBe(1);
    expect(disabledButtons[0].textContent).toContain('Shopping List');
  }));

  it('should call getRecipeById with correct identifier', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)), 'my-recipe-id');
    fixture.detectChanges();
    tick();

    expect(recipeApiMock.getRecipeById).toHaveBeenCalledWith('my-recipe-id');
  }));

  it('should show not-found when route has no identifier', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)), null as unknown as string);
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    expect(recipeApiMock.getRecipeById).not.toHaveBeenCalled();
    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Recipe not found.');
  }));

  it('should return null totalTime when both times are null', fakeAsync(() => {
    const noTimes = { ...mockRecipe, prepTimeMinutes: null, cookTimeMinutes: null };
    setupTestBed(vi.fn().mockReturnValue(of(noTimes)));
    fixture.detectChanges();
    tick();

    expect(component.totalTime()).toBeNull();
  }));

  it('should compute totalTime as sum of prep and cook', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component.totalTime()).toBe(30);
  }));

  it('should compute totalTime with only prep time', fakeAsync(() => {
    const prepOnly = { ...mockRecipe, cookTimeMinutes: null };
    setupTestBed(vi.fn().mockReturnValue(of(prepOnly)));
    fixture.detectChanges();
    tick();

    expect(component.totalTime()).toBe(10);
  }));

  it('should compute totalTime with only cook time', fakeAsync(() => {
    const cookOnly = { ...mockRecipe, prepTimeMinutes: null };
    setupTestBed(vi.fn().mockReturnValue(of(cookOnly)));
    fixture.detectChanges();
    tick();

    expect(component.totalTime()).toBe(20);
  }));

  it('should set isLoading to false after successful load', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component.isLoading()).toBe(false);
  }));

  it('should set isLoading to false after error', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 500 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();

    expect(component.isLoading()).toBe(false);
  }));

  it('should render delete button as enabled', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    expect(deleteButton).toBeTruthy();
    expect(deleteButton.disabled).toBe(false);
    expect(deleteButton.textContent.trim()).toBe('Delete');
  }));

  it('should show confirmation dialog on delete click', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false);

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    deleteButton.click();
    tick();

    expect(confirmSpy).toHaveBeenCalled();
    confirmSpy.mockRestore();
  }));

  it('should call deleteRecipe API when confirmed', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    recipeApiMock.deleteRecipe.mockReturnValue(of(undefined));

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    deleteButton.click();
    tick();

    expect(recipeApiMock.deleteRecipe).toHaveBeenCalledWith('abc-123');
    vi.restoreAllMocks();
  }));

  it('should NOT call deleteRecipe API when cancelled', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    vi.spyOn(window, 'confirm').mockReturnValue(false);

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    deleteButton.click();
    tick();

    expect(recipeApiMock.deleteRecipe).not.toHaveBeenCalled();
    vi.restoreAllMocks();
  }));

  it('should show deleting state while in progress', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const deleteSubject = new Subject<void>();
    recipeApiMock.deleteRecipe.mockReturnValue(deleteSubject);

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    deleteButton.click();
    fixture.detectChanges();

    expect(component.isDeleting()).toBe(true);
    expect(deleteButton.textContent.trim()).toBe('Deleting...');

    deleteSubject.next(undefined);
    deleteSubject.complete();
    tick();

    vi.restoreAllMocks();
  }));

  it('should navigate to /recipes on successful delete', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    recipeApiMock.deleteRecipe.mockReturnValue(of(undefined));
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    deleteButton.click();
    tick();

    expect(navigateSpy).toHaveBeenCalledWith(['/recipes']);
    vi.restoreAllMocks();
  }));

  it('should show error message on delete API failure', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const httpError = new HttpErrorResponse({ status: 500 });
    recipeApiMock.deleteRecipe.mockReturnValue(throwError(() => httpError));

    const deleteButton = fixture.nativeElement.querySelector(
      '.action-button--danger',
    );
    deleteButton.click();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to delete recipe.');
    vi.restoreAllMocks();
  }));
});
