export interface SaveRecipeRequest {
  title: string;
  description: string | null;
  ingredients: { name: string; amount: number | null; unit: string | null }[];
  steps: { number: number; description: string }[];
  servings: number | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  difficulty: string | null;
  imageUrl: string | null;
  sourceUrl?: string;
  tags?: string[];
}
