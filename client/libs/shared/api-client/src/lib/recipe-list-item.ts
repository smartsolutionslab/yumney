export interface RecipeListItem {
  identifier: string;
  title: string;
  description: string | null;
  servings: number | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  difficulty: string | null;
  imageUrl: string | null;
  createdAt: string;
  tags: string[];
  isFavorite: boolean;
}
