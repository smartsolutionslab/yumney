import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MealPlanApiService } from './meal-plan-api.service';
import { API_ENDPOINTS } from './api-endpoints';
import type {
  WeeklyPlan,
  AssignRecipeRequest,
  ClearSlotRequest,
  SwapSlotsRequest,
  AdjustServingsRequest,
  ConfirmMealRequest,
  CookWithLeftoversRequest,
  WeeklyPlannedRecipes,
  GenerateShoppingListResult,
  MealHistoryEntry,
} from './meal-plan';
import type { PagedResponse } from '@yumney/shared/models';

const year = 2026;
const week = 16;

const mockWeeklyPlan: WeeklyPlan = {
  week: '2026-W16',
  isExtendedMode: false,
  slots: [
    {
      day: 'Monday',
      mealType: 'Dinner',
      contentType: 'Recipe',
      state: 'Planned',
      recipeIdentifier: 'recipe-abc',
      recipeTitle: 'Pasta Carbonara',
      servings: 4,
      freetextLabel: null,
      leftoverSourceDay: null,
      leftoverSourceMealType: null,
      isEmpty: false,
    },
  ],
};

describe('MealPlanApiService', () => {
  let service: MealPlanApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(MealPlanApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('getWeeklyPlan', () => {
    it('should GET /api/v1/meal-plans/:year/w/:week', () => {
      service.getWeeklyPlan(year, week).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.byWeek(year, week));
      expect(req.request.method).toBe('GET');
      req.flush(mockWeeklyPlan);
    });
  });

  describe('assignRecipe', () => {
    it('should POST to /api/v1/meal-plans/:year/w/:week/slots', () => {
      const request: AssignRecipeRequest = {
        day: 'Monday',
        recipeIdentifier: 'recipe-abc',
        recipeTitle: 'Pasta Carbonara',
        mealType: 'Dinner',
        servings: 4,
      };

      service.assignRecipe(year, week, request).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.slots(year, week));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockWeeklyPlan);
    });
  });

  describe('clearSlot', () => {
    it('should DELETE /api/v1/meal-plans/:year/w/:week/slots with body', () => {
      const request: ClearSlotRequest = { day: 'Monday', mealType: 'Dinner' };

      service.clearSlot(year, week, request).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.slots(year, week));
      expect(req.request.method).toBe('DELETE');
      expect(req.request.body).toEqual(request);
      req.flush(mockWeeklyPlan);
    });
  });

  describe('swapSlots', () => {
    it('should PUT to /api/v1/meal-plans/:year/w/:week/slots/swap', () => {
      const request: SwapSlotsRequest = {
        sourceDay: 'Monday',
        targetDay: 'Tuesday',
        mealType: 'Dinner',
      };

      service.swapSlots(year, week, request).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.slotsSwap(year, week));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockWeeklyPlan);
    });
  });

  describe('adjustServings', () => {
    it('should PUT to /api/v1/meal-plans/:year/w/:week/slots/servings', () => {
      const request: AdjustServingsRequest = {
        day: 'Monday',
        mealType: 'Dinner',
        servings: 2,
      };

      service.adjustServings(year, week, request).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.slotsServings(year, week));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockWeeklyPlan);
    });
  });

  describe('confirmMeal', () => {
    it('should PUT to /api/v1/meal-plans/:year/w/:week/slots/confirm', () => {
      const request: ConfirmMealRequest = {
        day: 'Monday',
        mealType: 'Dinner',
        state: 'Cooked',
      };

      service.confirmMeal(year, week, request).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.slotsConfirm(year, week));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockWeeklyPlan);
    });
  });

  describe('toggleExtendedMode', () => {
    it('should PUT to /api/v1/meal-plans/:year/w/:week/extended-mode', () => {
      service.toggleExtendedMode(year, week, true).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.extendedMode(year, week));
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ enable: true });
      req.flush(mockWeeklyPlan);
    });
  });

  describe('cookWithLeftovers', () => {
    it('should POST to /api/v1/meal-plans/:year/w/:week/cook-with-leftovers', () => {
      const request: CookWithLeftoversRequest = {
        cookDay: 'Monday',
        recipeIdentifier: 'recipe-abc',
        recipeTitle: 'Pasta Carbonara',
        totalServings: 6,
        eatServings: 4,
        leftoverDay: 'Tuesday',
        mealType: 'Dinner',
      };

      service.cookWithLeftovers(year, week, request).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.cookWithLeftovers(year, week));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockWeeklyPlan);
    });
  });

  describe('getPlannedRecipes', () => {
    it('should GET /api/v1/meal-plans/:year/w/:week/planned-recipes', () => {
      const mockPlannedRecipes: WeeklyPlannedRecipes = {
        week: '2026-W16',
        recipes: [
          {
            recipeIdentifier: 'recipe-abc',
            recipeTitle: 'Pasta Carbonara',
            servings: 4,
            day: 'Monday',
            mealType: 'Dinner',
          },
        ],
      };

      service.getPlannedRecipes(year, week).subscribe((result) => {
        expect(result).toEqual(mockPlannedRecipes);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.plannedRecipes(year, week));
      expect(req.request.method).toBe('GET');
      req.flush(mockPlannedRecipes);
    });
  });

  describe('generateShoppingList', () => {
    it('should POST to /api/v1/meal-plans/:year/w/:week/generate-shopping-list', () => {
      const mockResult: GenerateShoppingListResult = {
        itemsAdded: 12,
        staplesSkipped: 3,
      };

      service.generateShoppingList(year, week).subscribe((result) => {
        expect(result).toEqual(mockResult);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.generateShoppingList(year, week));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockResult);
    });
  });

  describe('searchHistory', () => {
    const mockHistory: PagedResponse<MealHistoryEntry> = {
      items: [
        {
          recipeIdentifier: 'recipe-abc',
          recipeTitle: 'Lasagna',
          week: '2024-W12',
          day: 'Wednesday',
          mealType: 'Dinner',
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    };

    it('GETs /history/search with no params by default', () => {
      service.searchHistory().subscribe((result) => {
        expect(result).toEqual(mockHistory);
      });

      const req = httpTesting.expectOne((request) => request.url === API_ENDPOINTS.mealPlans.historySearch);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys().length).toBe(0);
      req.flush(mockHistory);
    });

    it('passes term, page, and pageSize as query params', () => {
      service.searchHistory({ term: 'lasagna', page: 2, pageSize: 25 }).subscribe();

      const req = httpTesting.expectOne(
        (request) => request.url === API_ENDPOINTS.mealPlans.historySearch && request.params.get('term') === 'lasagna',
      );
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('25');
      req.flush(mockHistory);
    });

    it('omits empty term from query string', () => {
      service.searchHistory({ term: '', pageSize: 10 }).subscribe();

      const req = httpTesting.expectOne((request) => request.url === API_ENDPOINTS.mealPlans.historySearch);
      expect(req.request.params.has('term')).toBe(false);
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush(mockHistory);
    });
  });

  describe('copyPlanToWeek', () => {
    it('POSTs to /meal-plans/:srcY/w/:srcW/copy-to/:dstY/:dstW', () => {
      service.copyPlanToWeek(2024, 12, 2026, 16).subscribe((result) => {
        expect(result).toEqual(mockWeeklyPlan);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.copyTo(2024, 12, 2026, 16));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockWeeklyPlan);
    });
  });

  describe('suggestWeekPlan', () => {
    it('POSTs to /meal-plans/:year/w/:week/suggest with empty body', () => {
      const mockSuggestion = {
        week: '2026-W16',
        entries: [
          {
            day: 'Monday',
            mealType: 'Dinner',
            recipeIdentifier: 'recipe-abc',
            recipeTitle: 'Pasta',
            freshnessLabel: null,
            reason: null,
          },
        ],
      };

      service.suggestWeekPlan(year, week).subscribe((result) => {
        expect(result).toEqual(mockSuggestion);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.mealPlans.suggest(year, week));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(mockSuggestion);
    });
  });

  describe('getMealAnalytics', () => {
    const mockAnalytics = {
      period: '2026-05',
      totalCooked: 10,
      totalSkipped: 1,
      uniqueRecipes: 6,
      mealsPerWeek: 2.3,
      discoveryRate: 2,
      topRecipes: [],
      categoryDistribution: [],
    };

    it('GETs /meal-plans/analytics with year only when month is omitted', () => {
      service.getMealAnalytics(2026).subscribe();

      const req = httpTesting.expectOne(
        (request) => request.url === API_ENDPOINTS.mealPlans.analytics && request.params.get('year') === '2026',
      );
      expect(req.request.params.has('month')).toBe(false);
      req.flush(mockAnalytics);
    });

    it('passes month as a query param when provided', () => {
      service.getMealAnalytics(2026, 5).subscribe();

      const req = httpTesting.expectOne((request) => request.url === API_ENDPOINTS.mealPlans.analytics);
      expect(req.request.params.get('year')).toBe('2026');
      expect(req.request.params.get('month')).toBe('5');
      req.flush(mockAnalytics);
    });
  });
});
