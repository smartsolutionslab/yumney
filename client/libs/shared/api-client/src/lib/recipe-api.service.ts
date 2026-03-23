import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '@yumney/shared/auth';
import { PagedResponse, PaginationParams } from '@yumney/shared/models';
import { API_ENDPOINTS } from './api-endpoints';

export interface ImportRecipeRequest {
  url: string;
}

export interface ImportStreamEvent {
  type: 'status' | 'chunk' | 'done' | 'fail';
  data: string;
}

export interface ExtractedIngredient {
  name: string;
  amount: number | null;
  unit: string | null;
}

export interface ExtractedStep {
  number: number;
  description: string;
}

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

export interface UpdateRecipeRequest {
  title: string;
  description: string | null;
  ingredients: { name: string; amount: number | null; unit: string | null }[];
  steps: { number: number; description: string }[];
  servings: number | null;
  prepTimeMinutes: number | null;
  cookTimeMinutes: number | null;
  difficulty: string | null;
  imageUrl: string | null;
  tags?: string[];
}

export interface SavedRecipeResponse {
  identifier: string;
  title: string;
  createdAt: string;
}

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
}

export type RecipeListResponse = PagedResponse<RecipeListItem>;

export interface GetRecipesParams extends PaginationParams {
  sortBy?: 'Name' | 'Date';
  search?: string;
}

export interface RecipeIngredient {
  name: string;
  amount: number | null;
  unit: string | null;
}

export interface RecipeStep {
  number: number;
  description: string;
}

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
}

@Injectable({ providedIn: 'root' })
export class RecipeApiService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  importRecipe(request: ImportRecipeRequest): Observable<ImportRecipeResponse> {
    return this.http.post<ImportRecipeResponse>(API_ENDPOINTS.recipes.import, request);
  }

  importRecipeStream(url: string): Observable<ImportStreamEvent> {
    return new Observable((subscriber) => {
      const abortController = new AbortController();

      this.fetchSseStream(url, abortController.signal, subscriber);

      return () => abortController.abort();
    });
  }

  private async fetchSseStream(
    url: string,
    signal: AbortSignal,
    subscriber: import('rxjs').Subscriber<ImportStreamEvent>,
  ): Promise<void> {
    const headers: Record<string, string> = { Accept: 'text/event-stream' };
    const token = this.auth.getAccessToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    let response: Response;
    try {
      response = await fetch(API_ENDPOINTS.recipes.importStream(url), { headers, signal });
    } catch {
      if (!signal.aborted) {
        subscriber.error(new Error('Connection failed'));
      }
      return;
    }

    if (!response.ok || !response.body) {
      subscriber.error(new Error(`HTTP ${response.status}`));
      return;
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) {
          break;
        }

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';

        let eventType: string | null = null;
        for (const line of lines) {
          if (line.startsWith('event: ')) {
            eventType = line.slice(7).trim();
          } else if (line.startsWith('data: ') && eventType) {
            const data = line.slice(6);
            const type = eventType as ImportStreamEvent['type'];

            subscriber.next({ type, data });

            if (type === 'done' || type === 'fail') {
              subscriber.complete();
              reader.cancel();
              return;
            }

            eventType = null;
          }
        }
      }

      subscriber.complete();
    } catch {
      if (!signal.aborted) {
        subscriber.error(new Error('Connection lost'));
      }
    }
  }

  importFromPhotos(photos: File[]): Observable<ImportRecipeResponse> {
    const formData = new FormData();
    photos.forEach((photo) => formData.append('photos', photo));
    return this.http.post<ImportRecipeResponse>(API_ENDPOINTS.recipes.importFromPhotos, formData);
  }

  saveRecipe(request: SaveRecipeRequest): Observable<SavedRecipeResponse> {
    return this.http.post<SavedRecipeResponse>(API_ENDPOINTS.recipes.base, request);
  }

  updateRecipe(identifier: string, request: UpdateRecipeRequest): Observable<RecipeDetail> {
    return this.http.put<RecipeDetail>(API_ENDPOINTS.recipes.byIdentifier(identifier), request);
  }

  deleteRecipe(identifier: string): Observable<void> {
    return this.http.delete<void>(API_ENDPOINTS.recipes.byIdentifier(identifier));
  }

  getRecipeById(identifier: string): Observable<RecipeDetail> {
    return this.http.get<RecipeDetail>(API_ENDPOINTS.recipes.byIdentifier(identifier));
  }

  getRecipes(params: GetRecipesParams = {}): Observable<RecipeListResponse> {
    return this.http.get<RecipeListResponse>(API_ENDPOINTS.recipes.base, {
      params: {
        ...(params.page != null && { page: params.page }),
        ...(params.pageSize != null && { pageSize: params.pageSize }),
        ...(params.sortBy != null && { sortBy: params.sortBy }),
        ...(params.sortDirection != null && { sortDirection: params.sortDirection }),
        ...(params.search != null && params.search !== '' && { search: params.search }),
      },
    });
  }
}
