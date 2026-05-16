import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UserPreferencesService } from '@yumney/shared/models';
import { RecipeDetail } from '../api';
import { RecipeScalingService } from './recipe-scaling.service';

function buildRecipe(overrides: Partial<RecipeDetail> = {}): RecipeDetail {
  return {
    identifier: 'r1',
    title: 'Test',
    description: null,
    ingredients: [
      { name: 'flour', amount: 200, unit: 'g' },
      { name: 'eggs', amount: 2, unit: null },
    ],
    steps: [],
    servings: 4,
    prepTimeMinutes: null,
    cookTimeMinutes: null,
    difficulty: null,
    imageUrl: null,
    rating: null,
    isFavorite: false,
    notes: null,
    sourceUrl: null,
    tags: [],
    ...overrides,
  } as RecipeDetail;
}

describe('RecipeScalingService', () => {
  function createService(): RecipeScalingService {
    TestBed.configureTestingModule({
      providers: [RecipeScalingService, { provide: UserPreferencesService, useValue: { preferredUnitSystem: signal('metric') } }],
    });
    return TestBed.inject(RecipeScalingService);
  }

  // Regression: the original implementation reassigned a `private recipe`
  // field inside `attach()`, leaving the `scaledIngredients` / `isScaled`
  // computeds bound to the placeholder signal created at field-init time
  // (or to whichever signal happened to be the first one the computed
  // evaluated against). On staging this surfaced as recipe-detail rendering
  // with an empty ingredients list — never recovering even after the API
  // returned the recipe payload.
  it('scaledIngredients reacts to changes on the attached recipe signal', () => {
    const service = createService();
    const recipeRef = signal<RecipeDetail | null>(null);

    service.attach(recipeRef);
    expect(service.scaledIngredients()).toEqual([]);

    recipeRef.set(buildRecipe({ servings: 4 }));

    expect(service.scaledIngredients()).toHaveLength(2);
    expect(service.scaledIngredients()[0].name).toBe('flour');
  });

  it('isScaled tracks servings changes after attach + recipe load', () => {
    const service = createService();
    const recipeRef = signal<RecipeDetail | null>(null);

    service.attach(recipeRef);
    recipeRef.set(buildRecipe({ servings: 4 }));
    service.initFor(buildRecipe({ servings: 4 }));

    expect(service.isScaled()).toBe(false);

    service.desiredServings.set(8);

    expect(service.isScaled()).toBe(true);
  });

  it('scaling halves ingredient amounts when desired servings is half the original', () => {
    const service = createService();
    const recipeRef = signal<RecipeDetail | null>(null);
    service.attach(recipeRef);
    const recipe = buildRecipe({ servings: 4 });
    recipeRef.set(recipe);
    service.initFor(recipe);

    service.desiredServings.set(2);

    const flour = service.scaledIngredients().find((entry) => entry.name === 'flour');
    expect(flour?.amount).toBe(100);
  });

  it('reset() restores the original servings count', () => {
    const service = createService();
    const recipeRef = signal<RecipeDetail | null>(null);
    service.attach(recipeRef);
    const recipe = buildRecipe({ servings: 4 });
    recipeRef.set(recipe);
    service.initFor(recipe);
    service.desiredServings.set(10);

    service.reset();

    expect(service.desiredServings()).toBe(4);
  });

  it('attach() with a different signal swaps the source the computeds track', () => {
    const service = createService();
    const firstRef = signal<RecipeDetail | null>(buildRecipe({ ingredients: [{ name: 'first', amount: 1, unit: 'g' }] }));
    const secondRef = signal<RecipeDetail | null>(buildRecipe({ ingredients: [{ name: 'second', amount: 2, unit: 'g' }] }));

    service.attach(firstRef);
    expect(service.scaledIngredients()[0].name).toBe('first');

    service.attach(secondRef);
    expect(service.scaledIngredients()[0].name).toBe('second');
  });
});
