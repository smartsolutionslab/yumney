import { DestroyRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RecipeApiService, RecipeDetail } from '../api';
import { RecipeNotesAutosaveService } from './recipe-notes-autosave.service';

function buildRecipe(overrides: Partial<RecipeDetail> = {}): RecipeDetail {
  return {
    identifier: 'r1',
    title: 'Test',
    description: null,
    ingredients: [],
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

describe('RecipeNotesAutosaveService', () => {
  function createService(api: Partial<RecipeApiService>): RecipeNotesAutosaveService {
    TestBed.configureTestingModule({
      providers: [
        RecipeNotesAutosaveService,
        { provide: RecipeApiService, useValue: api },
        { provide: DestroyRef, useValue: { onDestroy: () => () => undefined } },
      ],
    });
    return TestBed.inject(RecipeNotesAutosaveService);
  }

  it('seeds the draft from the attached recipe notes', () => {
    const service = createService({ updateRecipeNotes: vi.fn() });
    const recipeRef = signal<RecipeDetail | null>(buildRecipe({ notes: 'existing' }));

    service.attach(recipeRef);

    expect(service.draft()).toBe('existing');
  });

  it('treats absent notes as an empty draft', () => {
    const service = createService({ updateRecipeNotes: vi.fn() });
    const recipeRef = signal<RecipeDetail | null>(buildRecipe({ notes: null }));

    service.attach(recipeRef);

    expect(service.draft()).toBe('');
  });

  it('update() sets the draft and clears saved', () => {
    const service = createService({ updateRecipeNotes: vi.fn() });
    const recipeRef = signal<RecipeDetail | null>(buildRecipe({ notes: 'old' }));
    service.attach(recipeRef);
    service.saved.set(true);

    service.update('typed');

    expect(service.draft()).toBe('typed');
    expect(service.saved()).toBe(false);
  });
});
