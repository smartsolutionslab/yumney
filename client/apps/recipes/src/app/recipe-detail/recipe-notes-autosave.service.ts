import { Injectable, WritableSignal, inject, signal } from '@angular/core';
import { debouncedEffect } from '@yumney/shared/models';
import { RecipeApiService, RecipeDetail } from '../api';

const NOTES_AUTOSAVE_DEBOUNCE_MS = 400;
const SAVED_INDICATOR_MS = 2000;

@Injectable()
export class RecipeNotesAutosaveService {
  private api = inject(RecipeApiService);

  readonly draft = signal('');
  readonly saved = signal(false);
  private input = signal('');
  private recipeRef: WritableSignal<RecipeDetail | null> | null = null;

  constructor() {
    debouncedEffect(this.input, NOTES_AUTOSAVE_DEBOUNCE_MS, (value) => this.persist(value));
  }

  attach(recipeRef: WritableSignal<RecipeDetail | null>): void {
    this.recipeRef = recipeRef;
    this.draft.set(recipeRef()?.notes ?? '');
  }

  update(value: string): void {
    this.draft.set(value);
    this.saved.set(false);
    this.input.set(value);
  }

  private persist(value: string): void {
    const recipeRef = this.recipeRef;
    if (!recipeRef) return;
    const recipe = recipeRef();
    if (!recipe) return;
    const trimmed = value.trim();
    const payload = trimmed.length === 0 ? null : trimmed;
    if (payload === (recipe.notes ?? null)) return;

    this.api.updateRecipeNotes(recipe.identifier, payload).subscribe({
      next: () => {
        recipeRef.set({ ...recipe, notes: payload });
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), SAVED_INDICATOR_MS);
      },
    });
  }
}
