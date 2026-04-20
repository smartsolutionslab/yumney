import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { of, Subject, throwError, EMPTY } from 'rxjs';
import { RecipeDetailComponent } from './recipe-detail.component';
import { RecipeApiService, RecipeDetail } from '../api';
import { HttpErrorResponse } from '@angular/common/http';
import { setupTranslocoTesting } from '@yumney/shared/models';

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
        confirmButton: 'Delete',
        cancelButton: 'Cancel',
        deleting: 'Deleting...',
        success: 'Recipe deleted successfully.',
        errors: {
          notFound: 'Recipe not found.',
          generic: 'Failed to delete recipe. Please try again later.',
        },
      },
      resetServings: 'Reset',
      a11y: {
        decreaseServings: 'Decrease servings',
        increaseServings: 'Increase servings',
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
      imports: [RecipeDetailComponent, setupTranslocoTesting(en)],
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

    const title =
      fixture.nativeElement.querySelector('.hero-title') ??
      fixture.nativeElement.querySelector('.recipe-title');
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

    const editLink = fixture.nativeElement.querySelector('.btn-primary[href]');
    expect(editLink).toBeTruthy();
    expect(editLink.tagName).toBe('A');
    expect(editLink.getAttribute('href')).toBe('/recipes/abc-123/edit');
    expect(editLink.textContent.trim()).toBe('Edit');
  }));

  it('should render shopping list button as link', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const shoppingLink = fixture.nativeElement.querySelector('.btn-secondary');
    expect(shoppingLink).toBeTruthy();
    expect(shoppingLink.tagName).toBe('A');
    expect(shoppingLink.getAttribute('href')).toBe('/shopping/create/abc-123');
    expect(shoppingLink.textContent).toContain('Shopping List');
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

    const deleteButton = fixture.nativeElement.querySelector('.btn-danger');
    expect(deleteButton).toBeTruthy();
    expect(deleteButton.disabled).toBe(false);
    expect(deleteButton.textContent.trim()).toBe('Delete');
  }));

  it('should show confirmation dialog on delete click', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const deleteButton = fixture.nativeElement.querySelector('.btn-danger');
    deleteButton.click();
    fixture.detectChanges();

    expect(component.showDeleteConfirm()).toBe(true);
    const dialog = fixture.nativeElement.querySelector('yn-confirm-dialog');
    expect(dialog).toBeTruthy();
  }));

  it('should call deleteRecipe API when confirmed', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    recipeApiMock.deleteRecipe.mockReturnValue(of(undefined));

    component.onDeleteConfirmed();
    tick();

    expect(recipeApiMock.deleteRecipe).toHaveBeenCalledWith('abc-123');
  }));

  it('should NOT call deleteRecipe API when cancelled', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    component.onDelete();
    expect(component.showDeleteConfirm()).toBe(true);

    component.onDeleteCancelled();

    expect(component.showDeleteConfirm()).toBe(false);
    expect(recipeApiMock.deleteRecipe).not.toHaveBeenCalled();
  }));

  it('should show deleting state while in progress', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const deleteSubject = new Subject<void>();
    recipeApiMock.deleteRecipe.mockReturnValue(deleteSubject);

    component.onDeleteConfirmed();
    fixture.detectChanges();

    const deleteButton = fixture.nativeElement.querySelector('.btn-danger');
    expect(component.isDeleting()).toBe(true);
    expect(deleteButton.textContent.trim()).toBe('Deleting...');

    deleteSubject.next(undefined);
    deleteSubject.complete();
    tick();
  }));

  it('should navigate to /recipes on successful delete', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    recipeApiMock.deleteRecipe.mockReturnValue(of(undefined));
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component.onDeleteConfirmed();
    tick();

    expect(navigateSpy).toHaveBeenCalledWith(['/recipes']);
    navigateSpy.mockRestore();
  }));

  it('should show error message on delete API failure', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const httpError = new HttpErrorResponse({ status: 500 });
    recipeApiMock.deleteRecipe.mockReturnValue(throwError(() => httpError));

    component.onDeleteConfirmed();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to delete recipe.');
  }));

  it('should initialize desiredServings from recipe.servings', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component.desiredServings()).toBe(4);
  }));

  it('should increase servings on + click', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.onIncreaseServings();

    expect(component.desiredServings()).toBe(5);
  }));

  it('should decrease servings on - click', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.onDecreaseServings();

    expect(component.desiredServings()).toBe(3);
  }));

  it('should not decrease below 1', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.desiredServings.set(1);
    component.onDecreaseServings();

    expect(component.desiredServings()).toBe(1);
  }));

  it('should reset to original servings', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.desiredServings.set(8);
    component.onResetServings();

    expect(component.desiredServings()).toBe(4);
  }));

  it('should display scaled ingredient amounts', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.desiredServings.set(8);
    fixture.detectChanges();

    const scaled = component.scaledIngredients();
    expect(scaled[0].amount).toBe(800);
    expect(scaled[1].amount).toBe(8);
  }));

  it('should show isScaled as true when servings differ', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component.isScaled()).toBe(false);

    component.desiredServings.set(6);

    expect(component.isScaled()).toBe(true);
  }));
});
