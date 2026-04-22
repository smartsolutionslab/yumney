import type { ImportRecipeResponse, RecipeDetail } from '@yumney/shared/api-client';
import {
  mapDetailToImportResponse,
  mapToSaveRecipeRequest,
  mapToUpdateRecipeRequest,
} from './recipe-mapping';

const imported: ImportRecipeResponse = {
  title: 'Pasta Carbonara',
  description: 'Classic',
  ingredients: [
    { name: 'spaghetti', amount: 400, unit: 'g', confidence: 0.9 },
    { name: 'pancetta', amount: 150, unit: 'g', confidence: 0.95 },
  ],
  steps: [
    { number: 1, description: 'boil water', confidence: 1 },
    { number: 2, description: 'cook pasta', confidence: 1 },
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

  it('strips the extraction-confidence field from ingredients', () => {
    const result = mapToSaveRecipeRequest(imported);

    expect(result.ingredients[0]).toEqual({ name: 'spaghetti', amount: 400, unit: 'g' });
    expect(result.ingredients[0]).not.toHaveProperty('confidence');
  });

  it('strips the extraction-confidence field from steps', () => {
    const result = mapToSaveRecipeRequest(imported);

    expect(result.steps[0]).toEqual({ number: 1, description: 'boil water' });
    expect(result.steps[0]).not.toHaveProperty('confidence');
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
    };

    const result = mapDetailToImportResponse(detail);

    expect(result.title).toBe('X');
    expect(result.servings).toBe(2);
    expect(result.prepTimeMinutes).toBe(5);
    expect('sourceUrl' in result).toBe(false);
    expect('identifier' in result).toBe(false);
  });
});
