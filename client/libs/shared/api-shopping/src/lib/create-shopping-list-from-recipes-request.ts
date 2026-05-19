export interface CreateShoppingListFromRecipesRequest {
  title: string;
  recipes: Array<{
    recipeIdentifier: string;
    servings: number | null;
  }>;
}
