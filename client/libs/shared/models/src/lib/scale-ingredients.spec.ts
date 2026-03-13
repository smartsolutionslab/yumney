import {
  scaleIngredients,
  ScalableIngredient,
} from './scale-ingredients';

describe('scaleIngredients', () => {
  const baseIngredients: ScalableIngredient[] = [
    { name: 'Spaghetti', amount: 400, unit: 'g' },
    { name: 'Eggs', amount: 3, unit: null },
    { name: 'Olive Oil', amount: 1.5, unit: 'tbsp' },
    { name: 'Salt', amount: null, unit: null },
  ];

  it('should double amounts when servings doubled (4->8)', () => {
    const result = scaleIngredients(baseIngredients, 4, 8);

    expect(result[0].amount).toBe(800);
    expect(result[1].amount).toBe(6);
    expect(result[2].amount).toBe(3);
  });

  it('should halve amounts when servings halved (4->2)', () => {
    const result = scaleIngredients(baseIngredients, 4, 2);

    expect(result[0].amount).toBe(200);
    expect(result[1].amount).toBe(2);
    expect(result[2].amount).toBe(0.75);
  });

  it('should round whole-number amounts to whole numbers (3 eggs, 4->3 = 2)', () => {
    const result = scaleIngredients(baseIngredients, 4, 3);

    expect(result[1].amount).toBe(2);
  });

  it('should return original amounts when servings unchanged (4->4)', () => {
    const result = scaleIngredients(baseIngredients, 4, 4);

    expect(result).toBe(baseIngredients);
  });

  it('should handle null amounts (no amount ingredient)', () => {
    const result = scaleIngredients(baseIngredients, 4, 8);

    expect(result[3].amount).toBeNull();
    expect(result[3].name).toBe('Salt');
  });

  it('should round decimal amounts to 2 decimal places', () => {
    const ingredients: ScalableIngredient[] = [
      { name: 'Butter', amount: 1.5, unit: 'tbsp' },
    ];

    const result = scaleIngredients(ingredients, 4, 3);

    expect(result[0].amount).toBe(1.13);
  });

  it('should preserve ingredient names and units', () => {
    const result = scaleIngredients(baseIngredients, 4, 8);

    expect(result[0].name).toBe('Spaghetti');
    expect(result[0].unit).toBe('g');
    expect(result[1].name).toBe('Eggs');
    expect(result[1].unit).toBeNull();
  });
});
