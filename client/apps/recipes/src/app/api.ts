// MFE facade. This is the only place the recipes MFE imports from
// @yumney/shared/api-client. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports from the shared lib out of recipes/**.
export {
  RecipeApiService,
  MealPlanApiService,
  ShoppingApiService,
  type RecipeListItem,
  type RecipeListResponse,
  type RecipeDetail,
  type ImportRecipeResponse,
  type GetRecipesParams,
  type CreateShoppingListItem,
  type ShoppingListDetail,
} from '@yumney/shared/api-client';
