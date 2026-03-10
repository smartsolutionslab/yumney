import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
}

export interface RecipeListResponse {
  items: RecipeListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface GetRecipesParams {
  page?: number;
  pageSize?: number;
  sortBy?: 'Name' | 'Date';
  sortDirection?: 'Ascending' | 'Descending';
}

@Injectable({ providedIn: 'root' })
export class RecipeApiService {
  private http = inject(HttpClient);

  importRecipe(request: ImportRecipeRequest): Observable<ImportRecipeResponse> {
    return this.http.post<ImportRecipeResponse>('/api/v1/recipes/import', request);
  }

  saveRecipe(request: SaveRecipeRequest): Observable<SavedRecipeResponse> {
    return this.http.post<SavedRecipeResponse>('/api/v1/recipes', request);
  }

  getRecipes(params: GetRecipesParams = {}): Observable<RecipeListResponse> {
    return this.http.get<RecipeListResponse>('/api/v1/recipes', {
      params: {
        ...(params.page != null && { page: params.page }),
        ...(params.pageSize != null && { pageSize: params.pageSize }),
        ...(params.sortBy != null && { sortBy: params.sortBy }),
        ...(params.sortDirection != null && { sortDirection: params.sortDirection }),
      },
    });
  }
}
