export interface MealSlot {
  day: string;
  mealType: string;
  contentType: string;
  state: string;
  recipeIdentifier: string | null;
  recipeTitle: string | null;
  servings: number;
  freetextLabel: string | null;
  leftoverSourceDay: string | null;
  leftoverSourceMealType: string | null;
  isEmpty: boolean;
}

export interface WeeklyPlan {
  week: string;
  isExtendedMode: boolean;
  slots: MealSlot[];
}

export interface PlannedRecipe {
  recipeIdentifier: string;
  recipeTitle: string;
  servings: number;
  day: string;
  mealType: string;
}

export interface WeeklyPlannedRecipes {
  week: string;
  recipes: PlannedRecipe[];
}

export interface AssignRecipeRequest {
  day: string;
  recipeIdentifier: string;
  recipeTitle: string;
  mealType?: string;
  servings?: number;
}

export interface SwapSlotsRequest {
  sourceDay: string;
  targetDay: string;
  mealType?: string;
}

export interface ClearSlotRequest {
  day: string;
  mealType?: string;
}

export interface AdjustServingsRequest {
  day: string;
  mealType: string;
  servings: number;
}

export interface ConfirmMealRequest {
  day: string;
  mealType: string;
  state: 'Cooked' | 'Skipped' | 'Planned';
}

export interface CookWithLeftoversRequest {
  cookDay: string;
  recipeIdentifier: string;
  recipeTitle: string;
  totalServings: number;
  eatServings: number;
  leftoverDay: string;
  mealType?: string;
}
