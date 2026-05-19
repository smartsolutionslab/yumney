import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DashboardApiService } from './dashboard-api.service';
import { API_ENDPOINTS } from '@yumney/shared/api-common';
import type { UserActivityItem } from './user-activity';
import type { SuggestionsResponse } from './suggestion';

const mockActivity: UserActivityItem[] = [
  {
    type: 'RecipeImported',
    recipeIdentifier: 'recipe-abc',
    recipeTitle: 'Pasta Carbonara',
    occurredAt: '2026-04-15T10:00:00Z',
  },
  {
    type: 'RecipeCooked',
    recipeIdentifier: 'recipe-def',
    recipeTitle: 'Caesar Salad',
    occurredAt: '2026-04-14T18:30:00Z',
  },
];

const mockSuggestions: SuggestionsResponse = {
  suggestions: [
    {
      recipeIdentifier: 'recipe-abc',
      title: 'Pasta Carbonara',
      imageUrl: null,
      prepTimeMinutes: 10,
      reason: 'You liked similar recipes',
    },
  ],
  quickActions: ['Import a recipe', 'Plan your week'],
};

describe('DashboardApiService', () => {
  let service: DashboardApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DashboardApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('getRecentActivity', () => {
    it('should GET /api/v1/users/me/activity with default limit', () => {
      service.getRecentActivity().subscribe((result) => {
        expect(result).toEqual(mockActivity);
      });

      const req = httpTesting.expectOne((r) => r.url === API_ENDPOINTS.users.activity);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('limit')).toBe('5');
      req.flush(mockActivity);
    });

    it('should pass custom limit param', () => {
      service.getRecentActivity(10).subscribe((result) => {
        expect(result).toEqual(mockActivity);
      });

      const req = httpTesting.expectOne((r) => r.url === API_ENDPOINTS.users.activity);
      expect(req.request.params.get('limit')).toBe('10');
      req.flush(mockActivity);
    });

    it('should return empty array on error', () => {
      service.getRecentActivity().subscribe((result) => {
        expect(result).toEqual([]);
      });

      const req = httpTesting.expectOne((r) => r.url === API_ENDPOINTS.users.activity);
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });

  describe('getSuggestions', () => {
    it('should GET /api/v1/users/me/suggestions', () => {
      service.getSuggestions().subscribe((result) => {
        expect(result).toEqual(mockSuggestions);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.users.suggestions);
      expect(req.request.method).toBe('GET');
      req.flush(mockSuggestions);
    });

    it('should return fallback on error', () => {
      service.getSuggestions().subscribe((result) => {
        expect(result).toEqual({ suggestions: [], quickActions: [] });
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.users.suggestions);
      req.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
    });
  });
});
