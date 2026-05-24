import { ReadableStream } from 'node:stream/web';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '@yumney/shared/auth';
import { RecipeApiService } from './recipe-api.service';
import type { ImportRecipeRequest } from './import-recipe-request';
import type { ImportRecipeResponse } from './import-recipe-response';
import type { SaveRecipeRequest } from './save-recipe-request';
import type { SavedRecipeResponse } from './saved-recipe-response';
import type { UpdateRecipeRequest } from './update-recipe-request';
import type { RecipeDetail } from './recipe-detail';
import type { RecipeListResponse } from './recipe-list-response';

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
  tags: [],
  isFavorite: false,
  rating: null,
  notes: null,
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
        { provide: AuthService, useValue: { gatewayUrl: () => '', getAccessToken: () => 'test-token' } },
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

  describe('toggleFavorite', () => {
    it('should POST /api/v1/recipes/:identifier/favorite', () => {
      service.toggleFavorite('recipe-abc').subscribe();

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc/favorite');
      expect(req.request.method).toBe('POST');
      req.flush({ isFavorite: true });
    });

    it('should invalidate the recipe cache so a subsequent getRecipeById refetches (#427)', () => {
      // Prime the in-memory shareReplay cache.
      service.getRecipeById('recipe-abc').subscribe();
      httpTesting.expectOne('/api/v1/recipes/recipe-abc').flush(mockRecipeDetail);

      service.toggleFavorite('recipe-abc').subscribe();
      httpTesting.expectOne('/api/v1/recipes/recipe-abc/favorite').flush({ isFavorite: true });

      // After toggle, getRecipeById must hit the network again — the cached
      // observable would replay stale isFavorite without invalidation.
      service.getRecipeById('recipe-abc').subscribe();
      const refetch = httpTesting.expectOne('/api/v1/recipes/recipe-abc');
      expect(refetch.request.method).toBe('GET');
      refetch.flush({ ...mockRecipeDetail });
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
          tags: [],
          isFavorite: false,
          rating: null,
          hasNotes: false,
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
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

  describe('rateRecipe', () => {
    it('should POST rating to /api/v1/recipes/{id}/rating', () => {
      service.rateRecipe('recipe-abc', 4).subscribe();

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc/rating');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ rating: 4 });
      req.flush(null);
    });
  });

  describe('updateRecipeNotes', () => {
    it('should PUT notes to /api/v1/recipes/{id}/notes', () => {
      service.updateRecipeNotes('recipe-abc', 'cook longer next time').subscribe();

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc/notes');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ notes: 'cook longer next time' });
      req.flush(null);
    });

    it('should send null notes to clear them', () => {
      service.updateRecipeNotes('recipe-abc', null).subscribe();

      const req = httpTesting.expectOne('/api/v1/recipes/recipe-abc/notes');
      expect(req.request.body).toEqual({ notes: null });
      req.flush(null);
    });
  });

  describe('importRecipeStream', () => {
    // SSE parser regression: the eventType accumulator must persist across
    // ReadableStream.read() calls. The chefkoch JSON-LD fast-path on 2026-05-24
    // emitted a ~1.5 KB chunk event whose `event: chunk\n` line landed in one
    // network frame and the `data: {...}` line in the next — losing the type
    // (function-local variable) and dropping the event entirely. Verified end-
    // to-end against staging: extraction returned 200 + JSON-LD success, but
    // the UI never displayed the Review-Extracted-Recipe form because the
    // chunk + done events were silently swallowed.
    function makeFakeFetchResponse(chunks: string[]): Response {
      const encoder = new TextEncoder();
      const stream = new ReadableStream<Uint8Array>({
        async start(controller) {
          for (const chunk of chunks) {
            controller.enqueue(encoder.encode(chunk));
          }
          controller.close();
        },
      });
      return new Response(stream, { status: 200, headers: { 'content-type': 'text/event-stream' } });
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('emits all events when each one arrives in its own chunk (happy path)', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue(
        makeFakeFetchResponse([
          'event: status\ndata: Fetching page...\n\n',
          'event: status\ndata: Extracting recipe...\n\n',
          'event: chunk\ndata: {"title":"Carbonara"}\n\n',
          'event: done\ndata: {"title":"Carbonara"}\n\n',
        ]),
      );

      const received: { type: string; data: string }[] = [];
      await new Promise<void>((resolve, reject) => {
        service.importRecipeStream('https://example.com/recipe').subscribe({
          next: (evt) => received.push(evt),
          error: reject,
          complete: resolve,
        });
      });

      expect(received.map((evt) => evt.type)).toEqual(['status', 'status', 'chunk', 'done']);
      expect(received[2].data).toBe('{"title":"Carbonara"}');
    });

    it('emits the chunk event even when event: and data: lines arrive in separate reads', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue(
        makeFakeFetchResponse([
          // First read: only the event-type line — the data line is in the next frame.
          'event: chunk\n',
          // Second read: the data line + blank-line terminator.
          'data: {"title":"Bolognese","ingredients":[]}\n\n',
          // Plus the terminal event so the stream completes.
          'event: done\ndata: {"title":"Bolognese"}\n\n',
        ]),
      );

      const received: { type: string; data: string }[] = [];
      await new Promise<void>((resolve, reject) => {
        service.importRecipeStream('https://example.com/recipe').subscribe({
          next: (evt) => received.push(evt),
          error: reject,
          complete: resolve,
        });
      });

      // The chunk event MUST be present even though its event: and data: lines
      // arrived in different network frames. The pre-fix parser dropped it.
      const chunkEvent = received.find((evt) => evt.type === 'chunk');
      expect(chunkEvent).toBeDefined();
      expect(chunkEvent!.data).toBe('{"title":"Bolognese","ingredients":[]}');

      const doneEvent = received.find((evt) => evt.type === 'done');
      expect(doneEvent).toBeDefined();
    });
  });
});
