import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { ShoppingApiService, type RecipeDetail } from '../../api';
import { RecipeCreateShoppingListService } from './recipe-create-shopping-list.service';

const mockRecipe: RecipeDetail = {
  identifier: 'recipe-abc',
  title: 'Pasta Carbonara',
  description: null,
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
  sourceUrl: null,
  createdAt: '2026-03-10T00:00:00Z',
  ingredients: [],
  steps: [],
  tags: [],
  isFavorite: false,
  rating: null,
  notes: null,
};

describe('RecipeCreateShoppingListService', () => {
  let service: RecipeCreateShoppingListService;
  let shoppingApiMock: { createShoppingList: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(() => {
    shoppingApiMock = {
      createShoppingList: vi.fn().mockReturnValue(of({ identifier: 'list-123' })),
    };

    TestBed.configureTestingModule({
      providers: [RecipeCreateShoppingListService, provideRouter([]), { provide: ShoppingApiService, useValue: shoppingApiMock }],
    });

    service = TestBed.inject(RecipeCreateShoppingListService);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
  });

  describe('open', () => {
    it('clears the prior server error and opens the dialog', () => {
      service.serverError.set('previous.error');
      service.open();
      expect(service.showConfirm()).toBe(true);
      expect(service.serverError()).toBeNull();
    });
  });

  describe('cancel', () => {
    it('closes the dialog when not in flight', () => {
      service.open();
      service.cancel();
      expect(service.showConfirm()).toBe(false);
    });

    it('keeps the dialog open while a create is in flight', () => {
      const pending = new Subject<{ identifier: string }>();
      shoppingApiMock.createShoppingList.mockReturnValue(pending);

      service.open();
      service.confirm({ recipe: mockRecipe, desiredServings: 4, ingredients: [] });
      service.cancel();

      expect(service.showConfirm()).toBe(true);
      pending.next({ identifier: 'list-123' });
      pending.complete();
    });
  });

  describe('confirm', () => {
    it('forwards items to the shopping API verbatim', () => {
      service.confirm({
        recipe: mockRecipe,
        desiredServings: 4,
        ingredients: [
          { name: 'Spaghetti', amount: 400, unit: 'g' },
          { name: 'Pancetta', amount: 200, unit: 'g' },
        ],
      });

      expect(shoppingApiMock.createShoppingList).toHaveBeenCalledWith(
        expect.objectContaining({
          recipeIdentifier: 'recipe-abc',
          items: [
            { name: 'Spaghetti', amount: 400, unit: 'g' },
            { name: 'Pancetta', amount: 200, unit: 'g' },
          ],
        }),
      );
    });

    it('appends (xN) to the title whenever both servings are known', () => {
      service.confirm({ recipe: mockRecipe, desiredServings: 6, ingredients: [] });

      expect(shoppingApiMock.createShoppingList).toHaveBeenCalledWith(expect.objectContaining({ title: 'Pasta Carbonara (x6)' }));
    });

    it('omits the scaling suffix when the recipe has no native servings', () => {
      service.confirm({ recipe: { ...mockRecipe, servings: null }, desiredServings: 4, ingredients: [] });

      expect(shoppingApiMock.createShoppingList).toHaveBeenCalledWith(expect.objectContaining({ title: 'Pasta Carbonara' }));
    });

    it('navigates to the created list and closes the dialog on success', () => {
      service.open();
      service.confirm({ recipe: mockRecipe, desiredServings: null, ingredients: [] });

      expect(router.navigateByUrl).toHaveBeenCalledWith('/shopping/lists/list-123');
      expect(service.showConfirm()).toBe(false);
    });

    it('surfaces the mapped error and closes the dialog on failure', () => {
      shoppingApiMock.createShoppingList.mockReturnValue(throwError(() => ({ status: 500 })));
      service.open();

      service.confirm({ recipe: mockRecipe, desiredServings: null, ingredients: [] });

      expect(service.serverError()).not.toBeNull();
      expect(service.showConfirm()).toBe(false);
    });
  });
});
