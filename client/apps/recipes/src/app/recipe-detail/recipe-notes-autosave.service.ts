import { Injectable, WritableSignal, effect, inject, signal } from '@angular/core';
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
    let firstRun = true;
    effect((onCleanup) => {
      const value = this.input();
      if (firstRun) {
        firstRun = false;
        return;
      }
      const timeoutId = setTimeout(() => this.persist(value), NOTES_AUTOSAVE_DEBOUNCE_MS);
      onCleanup(() => clearTimeout(timeoutId));
    });
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
