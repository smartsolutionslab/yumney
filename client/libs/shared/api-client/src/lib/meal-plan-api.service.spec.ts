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
} from './meal-plan';

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
});
