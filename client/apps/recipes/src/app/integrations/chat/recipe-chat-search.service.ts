import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { ChatApiService, RecipeApiService, RecipeListItem, type ChatRecipeSuggestion } from '../../api';

interface FallbackParams {
  pageSize: number;
  sortBy: 'Name' | 'Date' | 'Rating';
  sortDirection: 'Ascending' | 'Descending';
}

export interface RecipeChatSearchResult {
  items: RecipeListItem[];
  fellBack: boolean;
}

@Injectable()
export class RecipeChatSearchService {
  private recipeApi = inject(RecipeApiService);
  private chatApi = inject(ChatApiService);

  /**
   * Conversational-query detector. Triggers the chat path when the user
   * types something that looks like an intent rather than a keyword: more
   * than 3 words, more than 20 chars, or contains a question/intent phrase.
   * Single-word queries like "Bolognese" stay on the keyword path.
   */
  isConversational(value: string): boolean {
    const wordCount = value.split(/\s+/).filter((word) => word.length > 0).length;
    if (wordCount > 3) return true;
    if (value.length > 20) return true;
    const lower = value.toLowerCase();
    const intentPhrases = ['what', 'how', 'show me', 'find me', 'i want', 'something', 'with'];
    return intentPhrases.some((phrase) => lower.includes(phrase));
  }

  /**
   * Conversational search: routes the query through the chat service (which
   * invokes the search_recipes SK tool internally) and hydrates each returned
   * suggestion into a full RecipeListItem for the grid. Falls back to keyword
   * search if the chat call fails — a transient LLM outage must not regress
   * baseline search. `fellBack` lets the caller turn off the AI-powered UI hint
   * when the fallback fires.
   */
  search(query: string, fallback: FallbackParams): Observable<RecipeChatSearchResult> {
    return this.chatApi.send({ message: query, history: [] }).pipe(
      switchMap((response) => this.hydrateSuggestions(response.suggestions)),
      map((items) => ({ items, fellBack: false }) satisfies RecipeChatSearchResult),
      catchError(() =>
        this.recipeApi
          .getRecipes({
            page: 1,
            pageSize: fallback.pageSize,
            sortBy: fallback.sortBy,
            sortDirection: fallback.sortDirection,
            search: query,
          })
          .pipe(map((response) => ({ items: response.items, fellBack: true }) satisfies RecipeChatSearchResult)),
      ),
    );
  }

  private hydrateSuggestions(suggestions: ChatRecipeSuggestion[]): Observable<RecipeListItem[]> {
    const validIds = suggestions.map((suggestion) => suggestion.recipeIdentifier).filter((id): id is string => id !== null);

    if (validIds.length === 0) return of([] as RecipeListItem[]);

    return forkJoin(
      validIds.map((id) =>
        this.recipeApi.getRecipeById(id).pipe(
          map(
            (detail): RecipeListItem => ({
              identifier: detail.identifier,
              title: detail.title,
              description: detail.description,
              servings: detail.servings,
              prepTimeMinutes: detail.prepTimeMinutes,
              cookTimeMinutes: detail.cookTimeMinutes,
              difficulty: detail.difficulty,
              imageUrl: detail.imageUrl,
              createdAt: detail.createdAt,
              tags: detail.tags,
              isFavorite: detail.isFavorite,
              rating: detail.rating,
              hasNotes: detail.notes !== null,
            }),
          ),
          // Per-suggestion failure shouldn't tank the AI search — skip the
          // missing one and surface the rest.
          catchError(() => of(null)),
        ),
      ),
    ).pipe(map((items) => items.filter((item): item is RecipeListItem => item !== null)));
  }
}
