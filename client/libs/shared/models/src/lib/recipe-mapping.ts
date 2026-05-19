import type { ImportRecipeResponse, RecipeDetail, SaveRecipeRequest, UpdateRecipeRequest } from '@yumney/shared/api-recipes';

function extractRecipeFields(recipe: ImportRecipeResponse) {
  return {
    title: recipe.title,
    description: recipe.description,
    ingredients: recipe.ingredients.map(({ name, amount, unit }) => ({ name, amount, unit })),
    steps: recipe.steps.map(({ number, description }) => ({ number, description })),
    servings: recipe.servings,
    prepTimeMinutes: recipe.prepTimeMinutes,
    cookTimeMinutes: recipe.cookTimeMinutes,
    difficulty: recipe.difficulty,
    imageUrl: recipe.imageUrl,
  };
}

export function mapToSaveRecipeRequest(recipe: ImportRecipeResponse, sourceUrl?: string): SaveRecipeRequest {
  return { ...extractRecipeFields(recipe), ...(sourceUrl != null && { sourceUrl }) };
}

export function mapToUpdateRecipeRequest(recipe: ImportRecipeResponse): UpdateRecipeRequest {
  return extractRecipeFields(recipe);
}

export function mapDetailToImportResponse(detail: RecipeDetail): ImportRecipeResponse {
  return {
    title: detail.title,
    description: detail.description,
    ingredients: detail.ingredients,
    steps: detail.steps,
    servings: detail.servings,
    prepTimeMinutes: detail.prepTimeMinutes,
    cookTimeMinutes: detail.cookTimeMinutes,
    difficulty: detail.difficulty,
    imageUrl: detail.imageUrl,
  };
}
