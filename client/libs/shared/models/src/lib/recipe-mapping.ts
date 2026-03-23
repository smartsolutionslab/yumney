import type {
  ImportRecipeResponse,
  RecipeDetail,
  SaveRecipeRequest,
  UpdateRecipeRequest,
} from '@yumney/shared/api-client';

export function mapToSaveRecipeRequest(
  recipe: ImportRecipeResponse,
  sourceUrl?: string,
): SaveRecipeRequest {
  const {
    title,
    description,
    ingredients,
    steps,
    servings,
    prepTimeMinutes,
    cookTimeMinutes,
    difficulty,
    imageUrl,
  } = recipe;

  return {
    title,
    description,
    ingredients: ingredients.map(({ name, amount, unit }) => ({ name, amount, unit })),
    steps: steps.map(({ number, description }) => ({ number, description })),
    servings,
    prepTimeMinutes,
    cookTimeMinutes,
    difficulty,
    imageUrl,
    ...(sourceUrl != null && { sourceUrl }),
  };
}

export function mapToUpdateRecipeRequest(recipe: ImportRecipeResponse): UpdateRecipeRequest {
  const {
    title,
    description,
    ingredients,
    steps,
    servings,
    prepTimeMinutes,
    cookTimeMinutes,
    difficulty,
    imageUrl,
  } = recipe;

  return {
    title,
    description,
    ingredients: ingredients.map(({ name, amount, unit }) => ({ name, amount, unit })),
    steps: steps.map(({ number, description }) => ({ number, description })),
    servings,
    prepTimeMinutes,
    cookTimeMinutes,
    difficulty,
    imageUrl,
  };
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
