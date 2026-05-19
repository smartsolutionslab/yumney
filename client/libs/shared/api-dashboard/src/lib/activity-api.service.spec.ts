import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivityApiService } from './activity-api.service';
import { API_ENDPOINTS } from '@yumney/shared/api-common';
import type { RecipeActivityStats, UserActivityPage } from './user-activity';

const mockPage: UserActivityPage = {
  items: [
    {
      type: 'recipe_imported',
      recipeIdentifier: 'recipe-abc',
      recipeTitle: 'Pasta Carbonara',
      occurredAt: '2026-04-15T10:00:00Z',
    },
  ],
  nextCursor: 'cursor-123',
};

const mockStats: RecipeActivityStats = {
  cookCount: 3,
  lastCookedAt: '2026-04-14T18:30:00Z',
  viewCount: 7,
};

describe('ActivityApiService', () => {
  let service: ActivityApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ActivityApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('getActivity', () => {
    it('GETs /users/me/activity without params when no options supplied', () => {
      service.getActivity().subscribe((result) => {
        expect(result).toEqual(mockPage);
      });

      const req = httpTesting.expectOne((r) => r.url === API_ENDPOINTS.users.activity);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys()).toEqual([]);
      req.flush(mockPage);
    });

    it('forwards limit, type and cursor as query params when supplied', () => {
      service.getActivity({ limit: 20, type: 'recipe_cooked', cursor: 'opaque-cursor' }).subscribe((result) => {
        expect(result).toEqual(mockPage);
      });

      const req = httpTesting.expectOne((r) => r.url === API_ENDPOINTS.users.activity);
      expect(req.request.params.get('limit')).toBe('20');
      expect(req.request.params.get('type')).toBe('recipe_cooked');
      expect(req.request.params.get('cursor')).toBe('opaque-cursor');
      req.flush(mockPage);
    });

    it('omits params that are explicitly undefined', () => {
      service.getActivity({ limit: 5 }).subscribe();

      const req = httpTesting.expectOne((r) => r.url === API_ENDPOINTS.users.activity);
      expect(req.request.params.get('limit')).toBe('5');
      expect(req.request.params.has('type')).toBe(false);
      expect(req.request.params.has('cursor')).toBe(false);
      req.flush(mockPage);
    });
  });

  describe('getRecipeStats', () => {
    it('GETs the recipe-scoped stats endpoint', () => {
      service.getRecipeStats('recipe-xyz').subscribe((result) => {
        expect(result).toEqual(mockStats);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.users.activityRecipeStats('recipe-xyz'));
      expect(req.request.method).toBe('GET');
      req.flush(mockStats);
    });
  });
});
