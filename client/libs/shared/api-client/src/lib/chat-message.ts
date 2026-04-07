export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

export interface ChatRecipeSuggestion {
  recipeIdentifier: string | null;
  title: string;
  reason: string | null;
}

export interface ChatRequest {
  message: string;
  history: ChatMessage[];
}

export interface ChatResponse {
  reply: string;
  suggestions: ChatRecipeSuggestion[];
}
