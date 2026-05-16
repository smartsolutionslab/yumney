import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { UserPreferencesService, scaleIngredients, toSystem, type UnitSystem, VALIDATION } from '@yumney/shared/models';
import { RecipeDetail } from '../api';

/**
 * Owns the servings-scaling + unit-system view state for the recipe-detail
 * screen. The recipe-detail component drives identity (route → load), this
 * service owns the toggles and the derived ingredient list:
 *
 * - desiredServings (writable, defaults to the recipe's saved servings)
 * - userUnitOverride (writable, transient — per-view override that wins
 *   over the user's profile default but is never persisted)
 * - scaledIngredients / displayedIngredients / isScaled (computed)
 *
 * `attach(recipe)` wires the upstream recipe signal. `initFor(recipe)` seeds
 * `desiredServings` from the loaded recipe.
 */
@Injectable()
export class RecipeScalingService {
  private preferences = inject(UserPreferencesService);

  private recipe: Signal<RecipeDetail | null> = signal(null);
  private userUnitOverride = signal<UnitSystem | null>(null);

  readonly desiredServings = signal<number | null>(null);
  readonly unitSystem = computed<UnitSystem>(() => this.userUnitOverride() ?? this.preferences.preferredUnitSystem());

  readonly scaledIngredients = computed(() => {
    const recipe = this.recipe();
    if (!recipe) return [];
    const { ingredients, servings } = recipe;
    const desired = this.desiredServings();
    if (!desired || !servings) return ingredients;
    return scaleIngredients(ingredients, servings, desired);
  });

  /**
   * Final ingredient list rendered in the template — applies the unit-system
   * toggle on top of the servings-scaled list. When the system is metric the
   * recipe ships as-stored; flipping to imperial routes each amount/unit pair
   * through unit-conversion (no-op for count/unknown units like "pinch" or
   * "clove", smart-rounded for grams/litres/etc.).
   */
  readonly displayedIngredients = computed(() => {
    const ingredients = this.scaledIngredients();
    const system = this.unitSystem();
    if (system === 'metric') return ingredients;
    return ingredients.map((ingredient) => {
      if (ingredient.amount == null) return ingredient;
      const converted = toSystem(ingredient.amount, ingredient.unit, system);
      return { ...ingredient, amount: converted.amount, unit: converted.unit || null };
    });
  });

  readonly isScaled = computed(() => {
    const recipe = this.recipe();
    const desired = this.desiredServings();
    return recipe?.servings != null && desired != null && desired !== recipe.servings;
  });

  attach(recipeRef: Signal<RecipeDetail | null>): void {
    this.recipe = recipeRef;
  }

  initFor(recipe: RecipeDetail): void {
    this.desiredServings.set(recipe.servings);
  }

  setUnitSystem(system: UnitSystem): void {
    this.userUnitOverride.set(system);
  }

  increase(): void {
    const current = this.desiredServings();
    if (current !== null) {
      this.desiredServings.set(current + 1);
    }
  }

  decrease(): void {
    const current = this.desiredServings();
    if (current !== null && current > VALIDATION.RECIPES.SERVINGS.MIN_VALUE) {
      this.desiredServings.set(current - 1);
    }
  }

  reset(): void {
    const recipe = this.recipe();
    if (recipe?.servings != null) {
      this.desiredServings.set(recipe.servings);
    }
  }
}
