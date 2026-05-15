import { IngredientRecognitionService } from './ingredient-recognition.service';
import type { RecognizedIngredient } from '@yumney/shared/api-client';

describe('IngredientRecognitionService', () => {
  let service: IngredientRecognitionService;

  beforeEach(() => {
    // Use Object.create to bypass DI — we only test pure methods
    service = Object.create(IngredientRecognitionService.prototype) as IngredientRecognitionService;
  });

  describe('mergeIngredients', () => {
    it('should add new ingredients to an empty list', () => {
      const incoming: RecognizedIngredient[] = [
        { name: 'Tomato', confidence: 0.9, category: 'produce' },
        { name: 'Onion', confidence: 0.8, category: 'produce' },
      ];

      const result = service.mergeIngredients([], incoming);

      expect(result.length).toBe(2);
      expect(result[0].name).toBe('Tomato');
    });

    it('should deduplicate by case-insensitive name', () => {
      const existing: RecognizedIngredient[] = [{ name: 'Tomato', confidence: 0.7, category: 'produce' }];
      const incoming: RecognizedIngredient[] = [{ name: 'tomato', confidence: 0.9, category: 'produce' }];

      const result = service.mergeIngredients(existing, incoming);

      expect(result.length).toBe(1);
    });

    it('should keep the higher confidence on duplicate', () => {
      const existing: RecognizedIngredient[] = [{ name: 'Tomato', confidence: 0.6, category: 'produce' }];
      const incoming: RecognizedIngredient[] = [{ name: 'Tomato', confidence: 0.95, category: 'produce' }];

      const result = service.mergeIngredients(existing, incoming);

      expect(result[0].confidence).toBe(0.95);
    });

    it('should not lower confidence on duplicate with lower score', () => {
      const existing: RecognizedIngredient[] = [{ name: 'Tomato', confidence: 0.95, category: 'produce' }];
      const incoming: RecognizedIngredient[] = [{ name: 'Tomato', confidence: 0.6, category: 'produce' }];

      const result = service.mergeIngredients(existing, incoming);

      expect(result[0].confidence).toBe(0.95);
    });

    it('should sort results by confidence descending', () => {
      const incoming: RecognizedIngredient[] = [
        { name: 'Onion', confidence: 0.5, category: 'produce' },
        { name: 'Tomato', confidence: 0.9, category: 'produce' },
        { name: 'Garlic', confidence: 0.7, category: 'produce' },
      ];

      const result = service.mergeIngredients([], incoming);

      expect(result.map((i) => i.name)).toEqual(['Tomato', 'Garlic', 'Onion']);
    });

    it('should merge mixed existing and incoming items', () => {
      const existing: RecognizedIngredient[] = [
        { name: 'Tomato', confidence: 0.9, category: 'produce' },
        { name: 'Onion', confidence: 0.8, category: 'produce' },
      ];
      const incoming: RecognizedIngredient[] = [
        { name: 'Garlic', confidence: 0.7, category: 'produce' },
        { name: 'Tomato', confidence: 0.85, category: 'produce' },
      ];

      const result = service.mergeIngredients(existing, incoming);

      expect(result.length).toBe(3);
      expect(result.find((i) => i.name === 'Tomato')?.confidence).toBe(0.9);
    });
  });

  describe('confidenceLevel', () => {
    it('should classify high confidence at 0.8 and above', () => {
      expect(service.confidenceLevel(0.9)).toBe('high');
      expect(service.confidenceLevel(0.8)).toBe('high');
      expect(service.confidenceLevel(1.0)).toBe('high');
    });

    it('should classify medium confidence between 0.5 and 0.8', () => {
      expect(service.confidenceLevel(0.7)).toBe('medium');
      expect(service.confidenceLevel(0.5)).toBe('medium');
    });

    it('should classify low confidence below 0.5', () => {
      expect(service.confidenceLevel(0.4)).toBe('low');
      expect(service.confidenceLevel(0.0)).toBe('low');
    });
  });
});
