import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ShoppingCreateComponent } from './shopping-create.component';
import {
  RecipeApiService,
  RecipeDetail,
  ShoppingApiService,
  ShoppingListDetail,
} from '../api';
import { HttpErrorResponse } from '@angular/common/http';
import { setupTranslocoTesting } from '@yumney/shared/models';

const mockRecipe: RecipeDetail = {
  identifier: 'recipe-abc',
  title: 'Pasta Carbonara',
  description: 'A classic Italian dish',
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
  sourceUrl: null,
  createdAt: '2026-03-10T00:00:00Z',
  ingredients: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
    { name: 'Parmesan', amount: 100, unit: 'g' },
  ],
  steps: [
    { number: 1, description: 'Cook pasta' },
    { number: 2, description: 'Mix eggs and cheese' },
  ],
};

const mockShoppingListDetail: ShoppingListDetail = {
  identifier: 'list-123',
  title: 'Pasta Carbonara',
  recipeIdentifier: 'recipe-abc',
  createdAt: '2026-03-10T00:00:00Z',
  items: [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 4, unit: null },
    { name: 'Parmesan', amount: 100, unit: 'g' },
  ],
};

const en = {
  shopping: {
    create: {
      title: 'Create Shopping List',
      back: 'Back to Recipe',
      listTitle: 'List Title',
      ingredients: 'Ingredients',
      selectAll: 'Select all',
      deselectAll: 'Deselect all',
      submit: 'Create Shopping List',
      creating: 'Creating...',
      loading: 'Loading recipe...',
      errors: {
        recipeNotFound: 'Recipe not found.',
        generic: 'Failed to load recipe.',
        createFailed: 'Failed to create shopping list.',
      },
    },
  },
};

describe('ShoppingCreateComponent', () => {
  let component: ShoppingCreateComponent;
  let fixture: ComponentFixture<ShoppingCreateComponent>;
  let recipeApiMock: { getRecipeById: ReturnType<typeof vi.fn> };
  let shoppingApiMock: { createShoppingList: ReturnType<typeof vi.fn> };

  function setupTestBed(
    getRecipeByIdReturn: ReturnType<typeof vi.fn> = vi.fn().mockReturnValue(of(mockRecipe)),
    recipeIdentifier = 'recipe-abc',
  ) {
    recipeApiMock = { getRecipeById: getRecipeByIdReturn };
    shoppingApiMock = {
      createShoppingList: vi.fn().mockReturnValue(of(mockShoppingListDetail)),
    };

    TestBed.configureTestingModule({
      imports: [ShoppingCreateComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: ShoppingApiService, useValue: shoppingApiMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'recipeIdentifier' ? recipeIdentifier : null),
              },
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(ShoppingCreateComponent);
    component = fixture.componentInstance;
  }

  it('should load recipe ingredients on init', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    expect(recipeApiMock.getRecipeById).toHaveBeenCalledWith('recipe-abc');
    expect(component.recipe()).toEqual(mockRecipe);
  }));

  it('should have all ingredients selected by default', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    expect(component.ingredientSelections()).toEqual([true, true, true]);
  }));

  it('should pre-fill title with recipe title', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    expect(component.title()).toBe('Pasta Carbonara');
  }));

  it('should toggle ingredient selection', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    component.onToggleIngredient(1);

    expect(component.ingredientSelections()).toEqual([true, false, true]);
  }));

  it('should deselect all ingredients', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    component.onDeselectAll();

    expect(component.ingredientSelections()).toEqual([false, false, false]);
  }));

  it('should select all ingredients', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    component.onDeselectAll();
    component.onSelectAll();

    expect(component.ingredientSelections()).toEqual([true, true, true]);
  }));

  it('should create list with only selected ingredients', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    component.onToggleIngredient(1);
    component.onCreateShoppingList();
    tick();

    expect(shoppingApiMock.createShoppingList).toHaveBeenCalledWith({
      title: 'Pasta Carbonara',
      items: [
        { name: 'Spaghetti', amount: 400, unit: 'g' },
        { name: 'Parmesan', amount: 100, unit: 'g' },
      ],
      recipeIdentifier: 'recipe-abc',
    });
  }));

  it('should navigate on successful creation', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    component.onCreateShoppingList();
    tick();

    expect(navigateSpy).toHaveBeenCalledWith('/shopping/lists/list-123');
    navigateSpy.mockRestore();
  }));

  it('should not create when no ingredients selected', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    component.onDeselectAll();
    component.onCreateShoppingList();
    tick();

    expect(shoppingApiMock.createShoppingList).not.toHaveBeenCalled();
  }));

  it('should show error on create API failure', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    const httpError = new HttpErrorResponse({ status: 500 });
    shoppingApiMock.createShoppingList.mockReturnValue(throwError(() => httpError));

    component.onCreateShoppingList();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Failed to create shopping list.');
  }));

  it('should show error when recipe not found', fakeAsync(() => {
    const httpError = new HttpErrorResponse({ status: 404 });
    setupTestBed(vi.fn().mockReturnValue(throwError(() => httpError)));
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const error = fixture.nativeElement.querySelector('.error-banner');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('Recipe not found.');
  }));

  it('should render ingredient checkboxes', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const checkboxes = fixture.nativeElement.querySelectorAll('input[type="checkbox"]');
    expect(checkboxes.length).toBe(3);
    expect(checkboxes[0].checked).toBe(true);
    expect(checkboxes[1].checked).toBe(true);
    expect(checkboxes[2].checked).toBe(true);
  }));

  it('should disable create button when no ingredients selected', fakeAsync(() => {
    setupTestBed();
    fixture.detectChanges();
    tick();

    component.onDeselectAll();
    fixture.detectChanges();

    expect(component.hasSelectedIngredients()).toBe(false);
  }));
});
