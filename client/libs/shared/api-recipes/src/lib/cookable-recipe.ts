export type CookableRecipeMatchTier = 'Full' | 'Near';

export interface CookableRecipeListItem {
  recipeIdentifier: string;
  title: string;
  imageUrl: string | null;
  servings: number | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  difficulty: string | null;
  ingredientCount: number;
  tier: CookableRecipeMatchTier;
  missingIngredients: string[];
}

export interface CookableRecipeListResponse {
  items: CookableRecipeListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface GetCookableRecipesParams {
  page?: number;
  pageSize?: number;
  fullMatchOnly?: boolean;
}
