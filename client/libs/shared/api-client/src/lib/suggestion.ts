export interface SuggestionItem {
  recipeIdentifier: string;
  title: string;
  imageUrl: string | null;
  prepTimeMinutes: number | null;
  reason: string;
}

export interface SuggestionsResponse {
  suggestions: SuggestionItem[];
  quickActions: string[];
}
