import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { shareReplay, tap } from 'rxjs/operators';
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
import type { CookableRecipeListResponse, GetCookableRecipesParams } from './cookable-recipe';
import type { RecognizedIngredientsResponse } from './recognized-ingredient';
import type { FavoriteState } from './favorite-state';

@Injectable({ providedIn: 'root' })
export class RecipeApiService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private recipeCache = new Map<string, Observable<RecipeDetail>>();

  importRecipe(request: ImportRecipeRequest): Observable<ImportRecipeResponse> {
    return this.http.post<ImportRecipeResponse>(API_ENDPOINTS.recipes.import, request);
  }

  private static readonly SSE_TIMEOUT_MS = 60_000;

  importRecipeStream(url: string): Observable<ImportStreamEvent> {
    return new Observable((subscriber) => {
      const abortController = new AbortController();
      // Have the timeout itself surface an error to subscribers. Without
      // this, the catch blocks in fetchSseStream / readSseBody guard
      // subscriber.error with `if (!signal.aborted)` — and a timeout-
      // induced abort sets signal.aborted=true, so the guard skips the
      // error and the Observable hangs forever (#430). Calling
      // subscriber.error from the timeout pre-empts the catch; if the
      // stream completed normally, RxJS makes subsequent error/complete
      // calls no-ops so this is safe.
      const timeout = setTimeout(() => {
        abortController.abort();
        // 504 mirrors the HTTP semantic: upstream took too long. Lets
        // consumers feed this straight into mapHttpError + ERROR_MAPS.
        subscriber.error(new HttpErrorResponse({ status: 504, statusText: 'Import timed out' }));
      }, RecipeApiService.SSE_TIMEOUT_MS);

      this.fetchSseStream(url, abortController, subscriber).finally(() => clearTimeout(timeout));

      return () => {
        clearTimeout(timeout);
        abortController.abort();
      };
    });
  }

  private async fetchSseStream(
    url: string,
    abortController: AbortController,
    subscriber: import('rxjs').Subscriber<ImportStreamEvent>,
  ): Promise<void> {
    const headers = this.buildSseHeaders();
    const { signal } = abortController;

    let response: Response;
    try {
      response = await fetch(API_ENDPOINTS.recipes.importStream(url), { headers, signal });
    } catch {
      if (!signal.aborted) {
        // 502 = couldn't reach the upstream (network failure before we got
        // any response). Lets mapHttpError pick the unreachable message.
        subscriber.error(new HttpErrorResponse({ status: 502, statusText: 'Connection failed' }));
      }
      return;
    }

    if (!response.ok || !response.body) {
      subscriber.error(new HttpErrorResponse({ status: response.status, statusText: response.statusText }));
      return;
    }

    await this.readSseBody(response.body, abortController, subscriber);
  }

  private buildSseHeaders(): Record<string, string> {
    const headers: Record<string, string> = { Accept: 'text/event-stream' };
    const token = this.auth.getAccessToken();
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    return headers;
  }

  private async readSseBody(
    body: ReadableStream<Uint8Array>,
    abortController: AbortController,
    subscriber: import('rxjs').Subscriber<ImportStreamEvent>,
  ): Promise<void> {
    const reader = body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';

        const terminal = this.parseSseBuffer(lines, subscriber);
        if (terminal) {
          await reader.cancel();
          abortController.abort();
          subscriber.complete();
          return;
        }
      }

      abortController.abort();
      subscriber.complete();
    } catch {
      if (!abortController.signal.aborted) {
        // Stream broke after we'd started reading. Same family as the
        // pre-response failure above: surface as 502 so consumers route it
        // through the unreachable error mapping.
        subscriber.error(new HttpErrorResponse({ status: 502, statusText: 'Connection lost' }));
      }
    }
  }

  private parseSseBuffer(lines: string[], subscriber: import('rxjs').Subscriber<ImportStreamEvent>): boolean {
    let eventType: string | null = null;

    for (const line of lines) {
      if (line.startsWith('event: ')) {
        eventType = line.slice(7).trim();
      } else if (line.startsWith('data: ') && eventType) {
        const type = eventType as ImportStreamEvent['type'];
        subscriber.next({ type, data: line.slice(6) });

        if (type === 'done' || type === 'fail') return true;
        eventType = null;
      }
    }

    return false;
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
    return this.http.post<RecognizedIngredientsResponse>(API_ENDPOINTS.recipes.recognizeIngredients, formData);
  }

  saveRecipe(request: SaveRecipeRequest): Observable<SavedRecipeResponse> {
    return this.http.post<SavedRecipeResponse>(API_ENDPOINTS.recipes.base, request);
  }

  updateRecipe(identifier: string, request: UpdateRecipeRequest): Observable<RecipeDetail> {
    return this.http
      .put<RecipeDetail>(API_ENDPOINTS.recipes.byIdentifier(identifier), request)
      .pipe(tap(() => this.invalidateRecipeCache(identifier)));
  }

  deleteRecipe(identifier: string): Observable<void> {
    return this.http.delete<void>(API_ENDPOINTS.recipes.byIdentifier(identifier)).pipe(tap(() => this.invalidateRecipeCache(identifier)));
  }

  getRecipeById(identifier: string): Observable<RecipeDetail> {
    if (!this.recipeCache.has(identifier)) {
      this.recipeCache.set(identifier, this.http.get<RecipeDetail>(API_ENDPOINTS.recipes.byIdentifier(identifier)).pipe(shareReplay(1)));
    }
    return this.recipeCache.get(identifier)!;
  }

  invalidateRecipeCache(identifier: string): void {
    this.recipeCache.delete(identifier);
  }

  getCookableRecipes(params: GetCookableRecipesParams = {}): Observable<CookableRecipeListResponse> {
    return this.http.get<CookableRecipeListResponse>(API_ENDPOINTS.recipes.whatCanICook, {
      params: {
        ...(params.page != null && { page: params.page }),
        ...(params.pageSize != null && { pageSize: params.pageSize }),
        ...(params.fullMatchOnly === true && { fullMatchOnly: true }),
      },
    });
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
        ...(params.favorites === true && { favorites: true }),
      },
    });
  }

  // Records a cook-mode completion (US-121). The server publishes a
  // RecipeCookedIntegrationEvent which the Users module persists as activity.
  trackCooked(identifier: string): Observable<void> {
    return this.http.post<void>(API_ENDPOINTS.recipes.cooked(identifier), {});
  }

  rateRecipe(identifier: string, rating: number): Observable<void> {
    return this.http
      .post<void>(API_ENDPOINTS.recipes.rating(identifier), { rating })
      .pipe(tap(() => this.invalidateRecipeCache(identifier)));
  }

  updateRecipeNotes(identifier: string, notes: string | null): Observable<void> {
    return this.http.put<void>(API_ENDPOINTS.recipes.notes(identifier), { notes }).pipe(tap(() => this.invalidateRecipeCache(identifier)));
  }

  toggleFavorite(identifier: string): Observable<FavoriteState> {
    // Invalidate the in-memory shareReplay cache so a subsequent
    // getRecipeById refetches with the new isFavorite — without this the
    // detail page replays the pre-toggle response and shows aria-pressed
    // wrong on the favorite button (#427).
    return this.http
      .post<FavoriteState>(API_ENDPOINTS.recipes.favorite(identifier), {})
      .pipe(tap(() => this.invalidateRecipeCache(identifier)));
  }
}
