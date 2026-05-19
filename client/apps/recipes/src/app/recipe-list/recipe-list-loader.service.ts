import { Injectable, inject, signal } from '@angular/core';
import { createAsyncState, ERROR_MAPS } from '@yumney/shared/models';
import { GetRecipesParams, RecipeApiService, RecipeListItem } from '../api';
import { RecipeChatSearchService } from '../integrations/chat/recipe-chat-search.service';

export interface ChatSearchOptions {
  pageSize: number;
  sortBy: 'Name' | 'Date' | 'Rating';
  sortDirection: 'Ascending' | 'Descending';
}

@Injectable()
export class RecipeListLoaderService {
  private recipeApi = inject(RecipeApiService);
  private chatSearch = inject(RecipeChatSearchService);
  private asyncState = createAsyncState();
  private loadRequestId = 0;

  readonly recipes = signal<RecipeListItem[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly availableTags = signal<string[]>([]);
  readonly isAiPowered = signal(false);
  readonly isLoading = this.asyncState.isLoading;
  readonly serverError = this.asyncState.serverError;

  reset(): void {
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
  }

  setAiPowered(value: boolean): void {
    this.isAiPowered.set(value);
  }

  load(params: GetRecipesParams, append: boolean): void {
    const requestId = ++this.loadRequestId;
    this.asyncState.execute(this.recipeApi.getRecipes(params), ERROR_MAPS.recipes.list, (response) => {
      if (requestId !== this.loadRequestId) return;
      this.totalCount.set(response.totalCount);
      if (append) {
        this.recipes.update((existing) => [...existing, ...response.items]);
      } else {
        this.recipes.set(response.items);
      }
      this.collectAvailableTags(response.items);
    });
  }

  loadFromChat(query: string, options: ChatSearchOptions): void {
    const requestId = ++this.loadRequestId;
    this.isAiPowered.set(true);
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);

    this.asyncState.execute(this.chatSearch.search(query, options), ERROR_MAPS.recipes.list, ({ items, fellBack }) => {
      if (requestId !== this.loadRequestId) return;
      if (fellBack) this.isAiPowered.set(false);
      this.recipes.set(items);
      this.totalCount.set(items.length);
      this.collectAvailableTags(items);
    });
  }

  private collectAvailableTags(items: RecipeListItem[]): void {
    const existing = new Set(this.availableTags());
    for (const item of items) {
      if (Array.isArray(item.tags)) {
        for (const tag of item.tags) existing.add(tag);
      }
    }
    this.availableTags.set([...existing].sort((a, b) => a.localeCompare(b)));
  }
}
