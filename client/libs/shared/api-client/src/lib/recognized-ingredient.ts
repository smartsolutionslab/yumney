export interface RecognizedIngredient {
  name: string;
  confidence: number;
  category: string | null;
}

export interface RecognizedIngredientsResponse {
  ingredients: RecognizedIngredient[];
}
