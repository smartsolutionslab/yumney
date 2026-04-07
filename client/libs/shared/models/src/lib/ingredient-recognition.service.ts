import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  RecipeApiService,
  type RecognizedIngredientsResponse,
  type RecognizedIngredient,
} from '@yumney/shared/api-client';

@Injectable({ providedIn: 'root' })
export class IngredientRecognitionService {
  private recipeApi = inject(RecipeApiService);

  recognize(photo: Blob): Observable<RecognizedIngredientsResponse> {
    return this.recipeApi.recognizeIngredients(photo);
  }

  /**
   * Merge a new recognition result into an existing list, deduplicating by name
   * (case-insensitive) and keeping the highest confidence score.
   */
  mergeIngredients(
    existing: RecognizedIngredient[],
    incoming: RecognizedIngredient[],
  ): RecognizedIngredient[] {
    const map = new Map<string, RecognizedIngredient>();

    for (const item of existing) {
      map.set(item.name.toLowerCase(), item);
    }

    for (const item of incoming) {
      const key = item.name.toLowerCase();
      const current = map.get(key);
      if (!current || item.confidence > current.confidence) {
        map.set(key, item);
      }
    }

    return Array.from(map.values()).sort((a, b) => b.confidence - a.confidence);
  }

  /**
   * Classify confidence score into a UI-friendly bucket.
   */
  confidenceLevel(confidence: number): 'high' | 'medium' | 'low' {
    if (confidence >= 0.8) return 'high';
    if (confidence >= 0.5) return 'medium';
    return 'low';
  }
}
