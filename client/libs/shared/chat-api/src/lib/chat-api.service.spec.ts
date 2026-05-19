import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { API_ENDPOINTS } from '@yumney/shared/api-common';
import { ChatApiService } from './chat-api.service';
import type { ChatRequest, ChatResponse } from './chat-message';

const mockChatResponse: ChatResponse = {
  reply: 'Here are some pasta recipes you might like.',
  suggestions: [
    {
      recipeIdentifier: 'recipe-abc',
      title: 'Pasta Carbonara',
      reason: 'Quick and classic',
    },
  ],
  actions: [],
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
});
