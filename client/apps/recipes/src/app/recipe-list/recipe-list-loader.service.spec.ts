import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { RecipeApiService, type RecipeListItem } from '../api';
import { RecipeChatSearchService } from '../integrations/chat/recipe-chat-search.service';
import { RecipeListLoaderService } from './recipe-list-loader.service';

const mockItems: RecipeListItem[] = [
  {
    identifier: 'r1',
    title: 'Carbonara',
    description: null,
    servings: 4,
    prepTimeMinutes: 10,
    cookTimeMinutes: 20,
    difficulty: 'medium',
    imageUrl: null,
    createdAt: '2026-04-10T00:00:00Z',
    tags: ['pasta', 'italian'],
    isFavorite: false,
    rating: null,
    hasNotes: false,
  },
  {
    identifier: 'r2',
    title: 'Salad',
    description: null,
    servings: 2,
    prepTimeMinutes: 5,
    cookTimeMinutes: null,
    difficulty: 'easy',
    imageUrl: null,
    createdAt: '2026-04-11T00:00:00Z',
    tags: ['vegetarian'],
    isFavorite: false,
    rating: null,
    hasNotes: false,
  },
];

describe('RecipeListLoaderService', () => {
  let loader: RecipeListLoaderService;
  let recipeApi: { getRecipes: ReturnType<typeof vi.fn> };
  let chatSearch: { search: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    recipeApi = { getRecipes: vi.fn().mockReturnValue(of({ items: mockItems, totalCount: 2 })) };
    chatSearch = { search: vi.fn().mockReturnValue(of({ items: mockItems.slice(0, 1), fellBack: false })) };

    TestBed.configureTestingModule({
      providers: [
        RecipeListLoaderService,
        { provide: RecipeApiService, useValue: recipeApi },
        { provide: RecipeChatSearchService, useValue: chatSearch },
      ],
    });

    loader = TestBed.inject(RecipeListLoaderService);
  });

  describe('load', () => {
    it('populates recipes + totalCount on success', () => {
      loader.load({ page: 1, pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' }, false);

      expect(loader.recipes()).toEqual(mockItems);
      expect(loader.totalCount()).toBe(2);
    });

    it('appends results when append=true', () => {
      loader.recipes.set(mockItems.slice(0, 1));
      recipeApi.getRecipes.mockReturnValue(of({ items: mockItems.slice(1), totalCount: 2 }));

      loader.load({ page: 2, pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' }, true);

      expect(loader.recipes()).toHaveLength(2);
    });

    it('collects unique tags sorted alphabetically', () => {
      loader.load({ page: 1, pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' }, false);

      expect(loader.availableTags()).toEqual(['italian', 'pasta', 'vegetarian']);
    });

    it('discards stale responses from earlier load calls', () => {
      const firstSubject = new Subject<{ items: RecipeListItem[]; totalCount: number }>();
      recipeApi.getRecipes.mockReturnValueOnce(firstSubject);

      loader.load({ page: 1, pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' }, false);
      // Second load runs synchronously via `of(...)`, bumping the requestId.
      loader.load({ page: 2, pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' }, false);

      firstSubject.next({ items: [mockItems[0]], totalCount: 99 });
      firstSubject.complete();

      expect(loader.totalCount()).toBe(2);
    });
  });

  describe('loadFromChat', () => {
    it('flips isAiPowered on and replaces the list with chat results', () => {
      loader.loadFromChat('quick dinner', { pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' });

      expect(loader.isAiPowered()).toBe(true);
      expect(loader.recipes()).toEqual(mockItems.slice(0, 1));
      expect(loader.totalCount()).toBe(1);
    });

    it('falls back to non-AI mode when chat reports fellBack', () => {
      chatSearch.search.mockReturnValue(of({ items: mockItems, fellBack: true }));

      loader.loadFromChat('anything', { pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' });

      expect(loader.isAiPowered()).toBe(false);
    });

    it('surfaces the chat error via serverError', () => {
      chatSearch.search.mockReturnValue(throwError(() => ({ status: 500 })));

      loader.loadFromChat('anything', { pageSize: 20, sortBy: 'Date', sortDirection: 'Descending' });

      expect(loader.serverError()).not.toBeNull();
    });
  });

  describe('reset', () => {
    it('clears recipes, totalCount, and resets currentPage to 1', () => {
      loader.recipes.set(mockItems);
      loader.totalCount.set(99);
      loader.currentPage.set(3);

      loader.reset();

      expect(loader.recipes()).toEqual([]);
      expect(loader.totalCount()).toBe(0);
      expect(loader.currentPage()).toBe(1);
    });
  });
});
