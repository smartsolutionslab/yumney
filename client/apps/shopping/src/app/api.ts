// MFE facade. This is the only place the shopping MFE imports from the
// per-backend api libs. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports out of shopping/**. Group new exports under the originating
// backend so the cross-MFE coupling stays visible at a glance.

// --- Shopping (primary surface) ---
export {
  ShoppingApiService,
  type ShoppingListDetail,
  type ShoppingListSummary,
  type ShoppingListItemResponse,
  type CreateShoppingListItem,
  type MergedShoppingList,
  type MergedShoppingItem,
  type ItemSource,
  type AddedItem,
  type Freshness,
  type IngredientBalance,
  type IngredientBalanceItem,
  type IngredientBalanceSource,
  type MarkAsFrozenRequest,
} from '@yumney/shared/api-shopping';

// --- Recipes (source recipe for the create-from-recipe flow) ---
export { RecipeApiService, type RecipeDetail } from '@yumney/shared/api-recipes';
