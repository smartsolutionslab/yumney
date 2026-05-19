import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { ChatApiService, type ChatAction, type ChatMessage, type ChatRecipeSuggestion } from '@yumney/shared/chat-api';
import { RecipeApiService } from '@yumney/shared/api-recipes';

export interface ChatDispatchResult {
  reply: string;
  suggestions: ChatRecipeSuggestion[];
  actions: ChatAction[];
}

@Injectable({ providedIn: 'root' })
export class ChatMessageDispatcher {
  private chatApi = inject(ChatApiService);
  private recipeApi = inject(RecipeApiService);

  dispatch(message: string, history: ChatMessage[]): Observable<ChatDispatchResult> {
    if (this.looksLikeUrl(message)) return this.dispatchUrlImport(message);
    if (this.looksLikeRecipeText(message)) return this.dispatchTextImport(message);
    return this.dispatchChat(message, history);
  }

  private dispatchChat(message: string, history: ChatMessage[]): Observable<ChatDispatchResult> {
    return this.chatApi.send({ message, history }).pipe(
      map((response) => ({
        reply: response.reply,
        suggestions: response.suggestions,
        actions: response.actions ?? [],
      })),
    );
  }

  private dispatchUrlImport(url: string): Observable<ChatDispatchResult> {
    return this.recipeApi.importRecipe({ url }).pipe(
      map((recipe) => ({
        reply: this.buildRecipeReply(recipe),
        suggestions: [{ recipeIdentifier: null, title: recipe.title, reason: 'Extracted from URL' }],
        actions: [],
      })),
    );
  }

  private dispatchTextImport(text: string): Observable<ChatDispatchResult> {
    return this.recipeApi.importFromText(text).pipe(
      map((recipe) => ({
        reply: this.buildRecipeReply(recipe),
        suggestions: [{ recipeIdentifier: null, title: recipe.title, reason: 'Extracted from text' }],
        actions: [],
      })),
    );
  }

  private buildRecipeReply(recipe: { title: string; ingredients: unknown[]; steps: unknown[] }): string {
    return `I found a recipe: **${recipe.title}**\n${recipe.ingredients.length} ingredients, ${recipe.steps.length} steps.`;
  }

  private looksLikeUrl(text: string): boolean {
    return /^https?:\/\/\S+$/i.test(text);
  }

  private looksLikeRecipeText(text: string): boolean {
    const lines = text.split('\n').length;
    const words = text.split(/\s+/).length;
    return lines >= 3 || words >= 30;
  }
}
