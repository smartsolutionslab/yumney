// MFE facade. This is the only place the recipes MFE imports from the
// per-backend api libs. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports out of recipes/**. Group new exports under the originating
// backend so the cross-MFE coupling stays visible at a glance.

// --- Recipes (primary surface) ---
export {
  RecipeApiService,
  type RecipeListItem,
  type RecipeListResponse,
  type RecipeDetail,
  type ImportRecipeResponse,
  type GetRecipesParams,
  type CookableRecipeListItem,
  type CookableRecipeListResponse,
  type CookableRecipeMatchTier,
  type GetCookableRecipesParams,
} from '@yumney/shared/api-recipes';

// --- MealPlan (assignment from recipe list) ---
export { MealPlanApiService } from '@yumney/shared/api-meal-plan';

// --- Shopping (create-shopping-list-from-recipes flow) ---
export { ShoppingApiService, type CreateShoppingListItem, type ShoppingListDetail } from '@yumney/shared/api-shopping';

// --- Dashboard (per-recipe activity stats on detail page) ---
export { ActivityApiService, type RecipeActivityStats } from '@yumney/shared/api-dashboard';

// --- Chat (conversational recipe search) ---
export { ChatApiService, type ChatRecipeSuggestion } from '@yumney/shared/chat-api';
