import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse, PaginationParams } from '@yumney/shared/models';
import { API_ENDPOINTS } from './api-endpoints';

export interface ImportRecipeRequest {
  url: string;
}

export interface ImportStreamEvent {
  type: 'status' | 'chunk' | 'done' | 'error';
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

  importRecipe(request: ImportRecipeRequest): Observable<ImportRecipeResponse> {
    return this.http.post<ImportRecipeResponse>(API_ENDPOINTS.recipes.import, request);
  }

  importRecipeStream(url: string): Observable<ImportStreamEvent> {
    return new Observable((subscriber) => {
      const eventSource = new EventSource(API_ENDPOINTS.recipes.importStream(url));

      eventSource.addEventListener('status', (e: MessageEvent) => {
        subscriber.next({ type: 'status', data: e.data });
      });

      eventSource.addEventListener('chunk', (e: MessageEvent) => {
        subscriber.next({ type: 'chunk', data: e.data });
      });

      eventSource.addEventListener('done', (e: MessageEvent) => {
        subscriber.next({ type: 'done', data: e.data });
        subscriber.complete();
        eventSource.close();
      });

      eventSource.addEventListener('error', (e: MessageEvent) => {
        subscriber.next({ type: 'error', data: e.data });
        subscriber.complete();
        eventSource.close();
      });

      eventSource.onerror = () => {
        subscriber.error(new Error('Connection lost'));
        eventSource.close();
      };

      return () => eventSource.close();
    });
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
