import type { RecipeIngredient } from './recipe-ingredient';
import type { RecipeStep } from './recipe-step';

export interface RecipeDetail {
  identifier: string;
  title: string;
  description: string | null;
  servings: number | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  difficulty: string | null;
  imageUrl: string | null;
  sourceUrl: string | null;
  createdAt: string;
  ingredients: RecipeIngredient[];
  steps: RecipeStep[];
  tags: string[];
  isFavorite: boolean;
  rating: number | null;
  notes: string | null;
}
