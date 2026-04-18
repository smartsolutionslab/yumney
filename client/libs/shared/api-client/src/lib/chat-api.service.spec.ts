import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ChatApiService } from './chat-api.service';
import { API_ENDPOINTS } from './api-endpoints';
import type { ChatRequest, ChatResponse } from './chat-message';
import type { ImportRecipeResponse } from './import-recipe-response';

const mockChatResponse: ChatResponse = {
  reply: 'Here are some pasta recipes you might like.',
  suggestions: [
    {
      recipeIdentifier: 'recipe-abc',
      title: 'Pasta Carbonara',
      reason: 'Quick and classic',
    },
  ],
};

const mockImportResponse: ImportRecipeResponse = {
  title: 'Homemade Pizza',
  description: 'A simple pizza recipe',
  ingredients: [{ name: 'Flour', amount: 500, unit: 'g' }],
  steps: [{ number: 1, description: 'Make dough' }],
  servings: 4,
  prepTimeMinutes: 30,
  cookTimeMinutes: 15,
  difficulty: 'medium',
  imageUrl: null,
};

describe('ChatApiService', () => {
  let service: ChatApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ChatApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('send', () => {
    it('should POST to /api/v1/recipes/chat', () => {
      const request: ChatRequest = {
        message: 'Suggest a pasta recipe',
        history: [
          { role: 'user', content: 'Hello' },
          { role: 'assistant', content: 'Hi! How can I help?' },
        ],
      };

      service.send(request).subscribe((result) => {
        expect(result).toEqual(mockChatResponse);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.recipes.chat);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockChatResponse);
    });
  });

  describe('importFromText', () => {
    it('should POST to /api/v1/recipes/import-from-text', () => {
      const text = '500g flour, 200ml water, mix and bake at 200C for 15 min';

      service.importFromText(text).subscribe((result) => {
        expect(result).toEqual(mockImportResponse);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.recipes.importFromText);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ text });
      req.flush(mockImportResponse);
    });
  });
});
