import { mergeRecipeIngredients, type RecipeForMerge } from './merge-recipe-ingredients';

describe('mergeRecipeIngredients', () => {
  it('sums amounts when name and unit match', () => {
    const result = mergeRecipeIngredients([
      recipe([{ name: 'Flour', amount: 200, unit: 'g' }], 4, 4),
      recipe([{ name: 'Flour', amount: 300, unit: 'g' }], 4, 4),
    ]);

    expect(result).toHaveLength(1);
    expect(result[0]).toEqual({ name: 'Flour', amount: 500, unit: 'g' });
  });

  it('keeps separate entries when units differ', () => {
    const result = mergeRecipeIngredients([
      recipe([{ name: 'Milk', amount: 2, unit: 'cup' }], 4, 4),
      recipe([{ name: 'Milk', amount: 500, unit: 'ml' }], 4, 4),
    ]);

    expect(result).toHaveLength(2);
    expect(result.find((entry) => entry.unit === 'cup')?.amount).toBe(2);
    expect(result.find((entry) => entry.unit === 'ml')?.amount).toBe(500);
  });

  it('treats name and unit casing as equivalent', () => {
    const result = mergeRecipeIngredients([
      recipe([{ name: 'flour', amount: 200, unit: 'g' }], 4, 4),
      recipe([{ name: 'FLOUR', amount: 300, unit: 'G' }], 4, 4),
    ]);

    expect(result).toHaveLength(1);
    expect(result[0].amount).toBe(500);
  });

  it('scales each recipe before merging', () => {
    const result = mergeRecipeIngredients([
      recipe([{ name: 'Onion', amount: 1, unit: null }], 4, 8),
      recipe([{ name: 'Onion', amount: 2, unit: null }], 4, 2),
    ]);

    expect(result).toHaveLength(1);
    expect(result[0].amount).toBe(3);
  });

  it('rounds non-integer scaled amounts to two decimals', () => {
    const result = mergeRecipeIngredients([
      recipe([{ name: 'Olive Oil', amount: 1.5, unit: 'tbsp' }], 4, 6),
    ]);

    expect(result[0].amount).toBe(2.25);
  });

  it('keeps amount-less ingredients with a null amount', () => {
    const result = mergeRecipeIngredients([
      recipe([{ name: 'Salt', amount: null, unit: null }], 4, 4),
      recipe([{ name: 'Salt', amount: null, unit: null }], 4, 4),
    ]);

    expect(result).toHaveLength(1);
    expect(result[0].amount).toBeNull();
  });

  it('drops scaling when servings are missing', () => {
    const result = mergeRecipeIngredients([
      {
        ingredients: [{ name: 'Flour', amount: 200, unit: 'g' }],
        originalServings: null,
        desiredServings: 8,
      },
    ]);

    expect(result[0].amount).toBe(200);
  });

  it('combines mixed cases of overlap and separate items', () => {
    const result = mergeRecipeIngredients([
      recipe(
        [
          { name: 'Flour', amount: 200, unit: 'g' },
          { name: 'Eggs', amount: 2, unit: null },
        ],
        4,
        4,
      ),
      recipe(
        [
          { name: 'Flour', amount: 300, unit: 'g' },
          { name: 'Milk', amount: 250, unit: 'ml' },
        ],
        4,
        4,
      ),
    ]);

    expect(result).toHaveLength(3);
    expect(result.find((entry) => entry.name === 'Flour')?.amount).toBe(500);
    expect(result.find((entry) => entry.name === 'Eggs')?.amount).toBe(2);
    expect(result.find((entry) => entry.name === 'Milk')?.amount).toBe(250);
  });
});

function recipe(
  ingredients: { name: string; amount: number | null; unit: string | null }[],
  originalServings: number,
  desiredServings: number,
): RecipeForMerge {
  return { ingredients, originalServings, desiredServings };
}
