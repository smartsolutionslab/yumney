import { scaleIngredients, type ScalableIngredient } from './scale-ingredients';

export interface RecipeForMerge {
  ingredients: readonly ScalableIngredient[];
  originalServings: number | null;
  desiredServings: number | null;
}

export interface MergedIngredient {
  name: string;
  amount: number | null;
  unit: string | null;
}

export function mergeRecipeIngredients(recipes: readonly RecipeForMerge[]): MergedIngredient[] {
  const buckets = new Map<string, MergedIngredient>();

  for (const recipe of recipes) {
    const scaled =
      recipe.originalServings !== null && recipe.desiredServings !== null
        ? scaleIngredients([...recipe.ingredients], recipe.originalServings, recipe.desiredServings)
        : recipe.ingredients;

    for (const ingredient of scaled) {
      const key = `${ingredient.name.trim().toLowerCase()}|${(ingredient.unit ?? '').trim().toLowerCase()}`;
      const existing = buckets.get(key);
      if (existing) {
        if (ingredient.amount !== null) {
          existing.amount = (existing.amount ?? 0) + ingredient.amount;
        }
      } else {
        buckets.set(key, {
          name: ingredient.name.trim(),
          unit: ingredient.unit?.trim() || null,
          amount: ingredient.amount,
        });
      }
    }
  }

  return [...buckets.values()];
}
