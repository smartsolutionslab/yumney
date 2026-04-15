import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '@yumney/shared/auth';
import {
  RecipeApiService,
  ImportRecipeRequest,
  ImportRecipeResponse,
  SaveRecipeRequest,
  SavedRecipeResponse,
  UpdateRecipeRequest,
  RecipeDetail,
  RecipeListResponse,
} from './recipe-api.service';

const mockImportResponse: ImportRecipeResponse = {
  title: 'Pasta Carbonara',
  description: 'A classic Italian dish',
  ingredients: [{ name: 'Spaghetti', amount: 400, unit: 'g' }],
  steps: [{ number: 1, description: 'Cook pasta' }],
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
};

const mockRecipeDetail: RecipeDetail = {
  identifier: 'recipe-abc',
  title: 'Pasta Carbonara',
  description: 'A classic Italian dish',
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'medium',
  imageUrl: null,
  sourceUrl: 'https://example.com/recipe',
  createdAt: '2026-03-10T00:00:00Z',
  ingredients: [{ name: 'Spaghetti', amount: 400, unit: 'g' }],
  steps: [{ number: 1, description: 'Cook pasta' }],
};

const mockSavedResponse: SavedRecipeResponse = {
  identifier: 'recipe-abc',
  title: 'Pasta Carbonara',
  createdAt: '2026-03-10T00:00:00Z',
};

describe('RecipeApiService', () => {
  let service: RecipeApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { gatewayUrl: () => '' } },
      ],
    });

    service = TestBed.inject(RecipeApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('importRecipe', () => {
    it('should POST to /api/v1/recipes/import', () => {
      const request: ImportRecipeRequest = { url: 'https://example.com/recipe' };

      service.importRecipe(request).subscribe((result) => {
        expect(result).toEqual(mockImportResponse);
      });

      const req = httpTesting.expectOne('/api/v1/recipes/import');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockImportResponse);
    });
  });

  describe('saveRecipe', () => {
    it('should POST to /api/v1/recipes', () => {
      const request: SaveRecipeRequest = {
        title: 'Pasta Carbonara',
        description: null,
        ingredients: [{ name: 'Spaghetti', amount: 400, unit: 'g' }],
        steps: [{ number: 1, description: 'Cook pasta' }],
        servings: 4,
        prepTimeMinutes: 10,
        cookTimeMinutes: 20,
        difficulty: 'medium',
        imageUrl: null,
        sourceUrl: 'https://example.com/recipe',
      };

      service.saveRecipe(request).subscribe((result) => {
        expect(result).toEqual(mockSavedResponse);
      });

      const req = httpTesting.expectOne('/api/v1/recipes');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockSavedResponse);
    });
  });

  describe('updateRecipe', () => {
    it('should PUT to /api/v1/recipes/:identifier', () => {
      const request: UpdateRecipeRequest = {
        title: 'Updated Pasta',
        description: null,
        ingredients: [{ name: 'Penne', amount: 500, unit: 'g' }],
        steps: [{ number: 1, description: 'Cook penne' }],
        servings: 2,
        prepTimeMinutes: 5,
        cookTimeMinutes: 15,
        difficulty: 'easy',
        imageUrl: null,
      };

      service.updateRecipe('recipe-abc', request).subscribe((result) => {
        expect(result).toEqual(mockRecipeDetail);
      });

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockRecipeDetail);
    });
  });

  describe('deleteRecipe', () => {
    it('should DELETE /api/v1/recipes/:identifier', () => {
      service.deleteRecipe('recipe-abc').subscribe();

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('getRecipeById', () => {
    it('should GET /api/v1/recipes/:identifier', () => {
      service.getRecipeById('recipe-abc').subscribe((result) => {
        expect(result).toEqual(mockRecipeDetail);
      });

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc');
      expect(req.request.method).toBe('GET');
      req.flush(mockRecipeDetail);
    });
  });

  describe('getRecipes', () => {
    const mockListResponse: RecipeListResponse = {
      items: [
        {
          identifier: 'recipe-abc',
          title: 'Pasta Carbonara',
          description: null,
          servings: 4,
          prepTimeMinutes: 10,
          cookTimeMinutes: 20,
          difficulty: 'medium',
          imageUrl: null,
          createdAt: '2026-03-10T00:00:00Z',
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    };

    it('should GET /api/v1/recipes with no params by default', () => {
      service.getRecipes().subscribe((result) => {
        expect(result).toEqual(mockListResponse);
      });

      const req = httpTesting.expectOne('/api/v1/recipes');
      expect(req.request.method).toBe('GET');
      req.flush(mockListResponse);
    });

    it('should pass pagination params when provided', () => {
      service.getRecipes({ page: 2, pageSize: 10 }).subscribe();

      const req = httpTesting.expectOne((r) => r.url === '/api/v1/recipes');
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush(mockListResponse);
    });

    it('should pass sortBy param when provided', () => {
      service.getRecipes({ sortBy: 'Name' }).subscribe();

      const req = httpTesting.expectOne((r) => r.url === '/api/v1/recipes');
      expect(req.request.params.get('sortBy')).toBe('Name');
      req.flush(mockListResponse);
    });

    it('should pass search param when provided', () => {
      service.getRecipes({ search: 'pasta' }).subscribe();

      const req = httpTesting.expectOne((r) => r.url === '/api/v1/recipes');
      expect(req.request.params.get('search')).toBe('pasta');
      req.flush(mockListResponse);
    });

    it('should not pass search param when it is empty string', () => {
      service.getRecipes({ search: '' }).subscribe();

      const req = httpTesting.expectOne((r) => r.url === '/api/v1/recipes');
      expect(req.request.params.has('search')).toBe(false);
      req.flush(mockListResponse);
    });
  });
});
