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
  sourceUrl: string;
}

export interface SavedRecipeResponse {
  identifier: string;
  title: string;
  importedAt: string;
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
}
