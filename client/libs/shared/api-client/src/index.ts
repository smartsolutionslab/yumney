// --- Auth ---
export { AuthApiService } from './lib/auth-api.service';
export type { RegisterRequest } from './lib/register-request';
export type { RegisterResponse } from './lib/register-response';
export type { ResendVerificationRequest } from './lib/resend-verification-request';
export type { ResendVerificationResponse } from './lib/resend-verification-response';

// --- Recipes ---
export { RecipeApiService } from './lib/recipe-api.service';
export type { ImportRecipeRequest } from './lib/import-recipe-request';
export type { ImportRecipeResponse } from './lib/import-recipe-response';
export type { ImportStreamEvent } from './lib/import-stream-event';
export type { ExtractedIngredient } from './lib/extracted-ingredient';
export type { ExtractedStep } from './lib/extracted-step';
export type { SaveRecipeRequest } from './lib/save-recipe-request';
export type { UpdateRecipeRequest } from './lib/update-recipe-request';
export type { SavedRecipeResponse } from './lib/saved-recipe-response';
export type { RecipeListItem } from './lib/recipe-list-item';
export type { RecipeListResponse } from './lib/recipe-list-response';
export type { GetRecipesParams } from './lib/get-recipes-params';
export type { FavoriteState } from './lib/favorite-state';
export type { RecipeDetail } from './lib/recipe-detail';
export type { RecipeIngredient } from './lib/recipe-ingredient';
export type { RecipeStep } from './lib/recipe-step';
export type {
  RecognizedIngredient,
  RecognizedIngredientsResponse,
} from './lib/recognized-ingredient';

// --- Shopping ---
export { ShoppingApiService } from './lib/shopping-api.service';
export type { CreateShoppingListItem } from './lib/create-shopping-list-item';
export type { CreateShoppingListRequest } from './lib/create-shopping-list-request';
export type { ShoppingListItemResponse } from './lib/shopping-list-item-response';
export type { ShoppingListDetail } from './lib/shopping-list-detail';
export type {
  MergedShoppingList,
  MergedShoppingItem,
  ItemSource,
  AddItemRequest,
  AddedItem,
  RemoveItemRequest,
} from './lib/merged-shopping-list';
export type { ShoppingListSummary } from './lib/shopping-list-summary';

// --- Chat ---
export { ChatApiService } from './lib/chat-api.service';
export type {
  ChatMessage,
  ChatRecipeSuggestion,
  ChatRequest,
  ChatResponse,
} from './lib/chat-message';

// --- Meal Plan ---
export { MealPlanApiService } from './lib/meal-plan-api.service';
export type {
  MealSlot,
  WeeklyPlan,
  PlannedRecipe,
  WeeklyPlannedRecipes,
  AssignRecipeRequest,
  SwapSlotsRequest,
  ClearSlotRequest,
  AdjustServingsRequest,
  ConfirmMealRequest,
  CookWithLeftoversRequest,
  GenerateShoppingListResult,
} from './lib/meal-plan';

// --- Dashboard ---
export { DashboardApiService } from './lib/dashboard-api.service';
export type { UserActivityItem } from './lib/user-activity';
export type { SuggestionItem, SuggestionsResponse } from './lib/suggestion';

// --- User Profile ---
export { UserProfileApiService } from './lib/user-profile-api.service';
export type { UserProfile, DietaryProfileDto, UpdateProfileRequest } from './lib/user-profile';
