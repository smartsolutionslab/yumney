import type { ExtractedIngredient } from './extracted-ingredient';
import type { ExtractedStep } from './extracted-step';

export interface ImportRecipeResponse {
  title: string;
  description: string | null;
  ingredients: ExtractedIngredient[];
  steps: ExtractedStep[];
  servings: number | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  difficulty: string | null;
  imageUrl: string | null;
}
