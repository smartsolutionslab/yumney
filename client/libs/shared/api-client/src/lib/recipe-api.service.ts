import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '@yumney/shared/auth';
import { API_ENDPOINTS } from './api-endpoints';
import type { ImportRecipeRequest } from './import-recipe-request';
import type { ImportRecipeResponse } from './import-recipe-response';
import type { ImportStreamEvent } from './import-stream-event';
import type { SaveRecipeRequest } from './save-recipe-request';
import type { UpdateRecipeRequest } from './update-recipe-request';
import type { SavedRecipeResponse } from './saved-recipe-response';
import type { RecipeDetail } from './recipe-detail';
import type { RecipeListResponse } from './recipe-list-response';
import type { GetRecipesParams } from './get-recipes-params';
import type { RecognizedIngredientsResponse } from './recognized-ingredient';

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

      this.fetchSseStream(url, abortController, subscriber);

      return () => abortController.abort();
    });
  }

  private async fetchSseStream(
    url: string,
    abortController: AbortController,
    subscriber: import('rxjs').Subscriber<ImportStreamEvent>,
  ): Promise<void> {
    const headers: Record<string, string> = { Accept: 'text/event-stream' };
    const token = this.auth.getAccessToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const signal = abortController.signal;

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
              await reader.cancel();
              abortController.abort();
              subscriber.complete();
              return;
            }

            eventType = null;
          }
        }
      }

      abortController.abort();
      subscriber.complete();
    } catch {
      if (!signal.aborted) {
        subscriber.error(new Error('Connection lost'));
      }
    }
  }

  importFromPhotos(photos: File[]): Observable<ImportRecipeResponse> {
    const formData = new FormData();
    for (const photo of photos) {
      formData.append('photos', photo);
    }
    return this.http.post<ImportRecipeResponse>(API_ENDPOINTS.recipes.importFromPhotos, formData);
  }

  recognizeIngredients(photo: Blob): Observable<RecognizedIngredientsResponse> {
    const formData = new FormData();
    formData.append('photo', photo, 'scan.jpg');
    return this.http.post<RecognizedIngredientsResponse>(
      API_ENDPOINTS.recipes.recognizeIngredients,
      formData,
    );
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
        ...(params.tags != null && params.tags.length > 0 && { tags: params.tags.join(',') }),
        ...(params.difficulty != null && { difficulty: params.difficulty }),
        ...(params.maxPrepTime != null && { maxPrepTime: params.maxPrepTime }),
        ...(params.maxCookTime != null && { maxCookTime: params.maxCookTime }),
      },
    });
  }
}
