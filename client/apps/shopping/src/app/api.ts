// MFE facade. This is the only place the shopping MFE imports from
// @yumney/shared/api-client. Grow this list as the MFE needs more; the
// no-restricted-imports rule in the root ESLint config keeps direct
// imports from the shared lib out of shopping/**.
export {
  RecipeApiService,
  ShoppingApiService,
  type RecipeDetail,
  type ShoppingListDetail,
  type ShoppingListSummary,
  type ShoppingListItemResponse,
  type CreateShoppingListItem,
  type MergedShoppingList,
  type MergedShoppingItem,
  type ItemSource,
  type AddedItem,
} from '@yumney/shared/api-client';
