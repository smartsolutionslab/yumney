// MFE facade. This is the only place the recipes MFE imports from the
// per-backend api libs. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports out of recipes/**.
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
export { MealPlanApiService } from '@yumney/shared/api-meal-plan';
export { ShoppingApiService, type CreateShoppingListItem, type ShoppingListDetail } from '@yumney/shared/api-shopping';
export { ActivityApiService, type RecipeActivityStats } from '@yumney/shared/api-dashboard';
export { ChatApiService, type ChatResponse, type ChatRecipeSuggestion } from '@yumney/shared/chat-api';
