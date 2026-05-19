import type { ImportRecipeResponse, RecipeDetail } from '@yumney/shared/api-recipes';
import { mapDetailToImportResponse, mapToSaveRecipeRequest, mapToUpdateRecipeRequest } from './recipe-mapping';

const imported: ImportRecipeResponse = {
  title: 'Pasta Carbonara',
  description: 'Classic',
  ingredients: [
    { name: 'spaghetti', amount: 400, unit: 'g' },
    { name: 'pancetta', amount: 150, unit: 'g' },
  ],
  steps: [
    { number: 1, description: 'boil water' },
    { number: 2, description: 'cook pasta' },
  ],
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 15,
  difficulty: 'medium',
  imageUrl: 'https://example.com/img.jpg',
};

describe('mapToSaveRecipeRequest', () => {
  it('copies the whitelisted fields from the import response', () => {
    const result = mapToSaveRecipeRequest(imported);

    expect(result.title).toBe('Pasta Carbonara');
    expect(result.description).toBe('Classic');
    expect(result.servings).toBe(4);
    expect(result.prepTimeMinutes).toBe(10);
    expect(result.cookTimeMinutes).toBe(15);
    expect(result.difficulty).toBe('medium');
    expect(result.imageUrl).toBe('https://example.com/img.jpg');
  });

  it('copies ingredients through to the request', () => {
    const result = mapToSaveRecipeRequest(imported);

    expect(result.ingredients[0]).toEqual({ name: 'spaghetti', amount: 400, unit: 'g' });
  });

  it('copies steps through to the request', () => {
    const result = mapToSaveRecipeRequest(imported);

    expect(result.steps[0]).toEqual({ number: 1, description: 'boil water' });
  });

  it('includes sourceUrl when provided', () => {
    const result = mapToSaveRecipeRequest(imported, 'https://source.example/r');

    expect(result.sourceUrl).toBe('https://source.example/r');
  });

  it('omits sourceUrl when undefined', () => {
    const result = mapToSaveRecipeRequest(imported);

    expect('sourceUrl' in result).toBe(false);
  });
});

describe('mapToUpdateRecipeRequest', () => {
  it('produces the same shape as save, without sourceUrl', () => {
    const result = mapToUpdateRecipeRequest(imported);

    expect('sourceUrl' in result).toBe(false);
    expect(result.title).toBe('Pasta Carbonara');
    expect(result.ingredients).toHaveLength(2);
  });
});

describe('mapDetailToImportResponse', () => {
  it('passes recipe fields through unchanged', () => {
    const detail: RecipeDetail = {
      identifier: 'abc',
      title: 'X',
      description: null,
      ingredients: [],
      steps: [],
      servings: 2,
      prepTimeMinutes: 5,
      cookTimeMinutes: null,
      difficulty: null,
      imageUrl: null,
      sourceUrl: null,
      tags: [],
      createdAt: '2026-04-22T00:00:00Z',
      isFavorite: false,
      rating: null,
      notes: null,
    };

    const result = mapDetailToImportResponse(detail);

    expect(result.title).toBe('X');
    expect(result.servings).toBe(2);
    expect(result.prepTimeMinutes).toBe(5);
    expect('sourceUrl' in result).toBe(false);
    expect('identifier' in result).toBe(false);
  });
});
