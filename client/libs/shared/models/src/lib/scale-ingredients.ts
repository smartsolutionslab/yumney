export interface ScalableIngredient {
  name: string;
  amount: number | null;
  unit: string | null;
}

function roundAmount(scaled: number, original: number): number {
  if (Number.isInteger(original)) {
    return Math.round(scaled);
  }
  return Math.round(scaled * 100) / 100;
}

export function scaleIngredients(
  ingredients: ScalableIngredient[],
  originalServings: number,
  desiredServings: number,
): ScalableIngredient[] {
  if (originalServings <= 0 || originalServings === desiredServings) {
    return ingredients;
  }
  const ratio = desiredServings / originalServings;
  return ingredients.map((ingredient) => ({
    ...ingredient,
    amount: ingredient.amount !== null ? roundAmount(ingredient.amount * ratio, ingredient.amount) : null,
  }));
}
