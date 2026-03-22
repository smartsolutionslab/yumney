import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse, PaginationParams } from '@yumney/shared/models';

export interface ImportRecipeRequest {
  url: string;
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
    return this.http.post<ImportRecipeResponse>('/api/v1/recipes/import', request);
  }

  importFromPhotos(photos: File[]): Observable<ImportRecipeResponse> {
    const formData = new FormData();
    photos.forEach((photo) => formData.append('photos', photo));
    return this.http.post<ImportRecipeResponse>('/api/v1/recipes/import-from-photos', formData);
  }

  saveRecipe(request: SaveRecipeRequest): Observable<SavedRecipeResponse> {
    return this.http.post<SavedRecipeResponse>('/api/v1/recipes', request);
  }

  updateRecipe(identifier: string, request: UpdateRecipeRequest): Observable<RecipeDetail> {
    return this.http.put<RecipeDetail>(`/api/v1/recipes/${identifier}`, request);
  }

  deleteRecipe(identifier: string): Observable<void> {
    return this.http.delete<void>(`/api/v1/recipes/${identifier}`);
  }

  getRecipeById(identifier: string): Observable<RecipeDetail> {
    return this.http.get<RecipeDetail>(`/api/v1/recipes/${identifier}`);
  }

  getRecipes(params: GetRecipesParams = {}): Observable<RecipeListResponse> {
    return this.http.get<RecipeListResponse>('/api/v1/recipes', {
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
