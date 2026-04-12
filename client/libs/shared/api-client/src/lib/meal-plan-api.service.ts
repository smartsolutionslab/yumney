import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type {
  WeeklyPlan,
  WeeklyPlannedRecipes,
  AssignRecipeRequest,
  SwapSlotsRequest,
  ClearSlotRequest,
  AdjustServingsRequest,
  ConfirmMealRequest,
  CookWithLeftoversRequest,
  GenerateShoppingListResult,
} from './meal-plan';

@Injectable({ providedIn: 'root' })
export class MealPlanApiService {
  private http = inject(HttpClient);

  getWeeklyPlan(year: number, week: number): Observable<WeeklyPlan> {
    return this.http.get<WeeklyPlan>(API_ENDPOINTS.mealPlans.byWeek(year, week));
  }

  assignRecipe(year: number, week: number, request: AssignRecipeRequest): Observable<WeeklyPlan> {
    return this.http.post<WeeklyPlan>(API_ENDPOINTS.mealPlans.slots(year, week), request);
  }

  clearSlot(year: number, week: number, request: ClearSlotRequest): Observable<WeeklyPlan> {
    return this.http.delete<WeeklyPlan>(API_ENDPOINTS.mealPlans.slots(year, week), {
      body: request,
    });
  }

  swapSlots(year: number, week: number, request: SwapSlotsRequest): Observable<WeeklyPlan> {
    return this.http.put<WeeklyPlan>(API_ENDPOINTS.mealPlans.slotsSwap(year, week), request);
  }

  adjustServings(
    year: number,
    week: number,
    request: AdjustServingsRequest,
  ): Observable<WeeklyPlan> {
    return this.http.put<WeeklyPlan>(API_ENDPOINTS.mealPlans.slotsServings(year, week), request);
  }

  confirmMeal(year: number, week: number, request: ConfirmMealRequest): Observable<WeeklyPlan> {
    return this.http.put<WeeklyPlan>(API_ENDPOINTS.mealPlans.slotsConfirm(year, week), request);
  }

  toggleExtendedMode(year: number, week: number, enable: boolean): Observable<WeeklyPlan> {
    return this.http.put<WeeklyPlan>(API_ENDPOINTS.mealPlans.extendedMode(year, week), {
      enable,
    });
  }

  cookWithLeftovers(
    year: number,
    week: number,
    request: CookWithLeftoversRequest,
  ): Observable<WeeklyPlan> {
    return this.http.post<WeeklyPlan>(
      API_ENDPOINTS.mealPlans.cookWithLeftovers(year, week),
      request,
    );
  }

  getPlannedRecipes(year: number, week: number): Observable<WeeklyPlannedRecipes> {
    return this.http.get<WeeklyPlannedRecipes>(API_ENDPOINTS.mealPlans.plannedRecipes(year, week));
  }

  generateShoppingList(year: number, week: number): Observable<GenerateShoppingListResult> {
    return this.http.post<GenerateShoppingListResult>(
      API_ENDPOINTS.mealPlans.generateShoppingList(year, week),
      {},
    );
  }
}
