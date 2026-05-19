import { Component, ChangeDetectionStrategy, computed, inject, OnInit, DestroyRef, signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { RecipeApiService, GetRecipesParams } from '../api';
import { debouncedEffect, ROUTES, UI, toggleFavoriteInList } from '@yumney/shared/models';
import { RouterLink } from '@angular/router';
import {
  ButtonComponent,
  EMPTY_FILTER,
  EmptyStateComponent,
  FilterPanelComponent,
  InfiniteScrollDirective,
  MessageBannerComponent,
  type RecipeFilterValue,
  StaggerNewItemsDirective,
} from '@yumney/ui';
import { RecipeCardComponent } from './recipe-card/recipe-card.component';
import { SortMenuComponent, SortMenuOption } from './sort-menu/sort-menu.component';
import { RecipeListLoaderService } from './recipe-list-loader.service';
import { RecipeAssignmentService } from '../integrations/meal-plan/recipe-assignment.service';
import { MultiRecipePreviewDialogComponent } from '../integrations/shopping/multi-recipe-preview-dialog/multi-recipe-preview-dialog.component';
import { MultiRecipeShoppingListService } from '../integrations/shopping/multi-recipe-shopping-list.service';
import { RecipeChatSearchService } from '../integrations/chat/recipe-chat-search.service';

interface SortOption extends SortMenuOption {
  by: 'Name' | 'Date' | 'Rating';
  dir: 'Ascending' | 'Descending';
}

@Component({
  selector: 'yn-recipe-list',
  imports: [
    TranslocoModule,
    RouterLink,
    LucideAngularModule,
    InfiniteScrollDirective,
    StaggerNewItemsDirective,
    ButtonComponent,
    EmptyStateComponent,
    FilterPanelComponent,
    MessageBannerComponent,
    RecipeCardComponent,
    SortMenuComponent,
    MultiRecipePreviewDialogComponent,
  ],
  providers: [RecipeAssignmentService, MultiRecipeShoppingListService, RecipeChatSearchService, RecipeListLoaderService],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private static readonly sortOptionList: readonly SortOption[] = [
    { value: 'date-desc', by: 'Date', dir: 'Descending', labelKey: 'recipes.list.sort.dateDesc' },
    { value: 'date-asc', by: 'Date', dir: 'Ascending', labelKey: 'recipes.list.sort.dateAsc' },
    { value: 'name-asc', by: 'Name', dir: 'Ascending', labelKey: 'recipes.list.sort.nameAsc' },
    { value: 'name-desc', by: 'Name', dir: 'Descending', labelKey: 'recipes.list.sort.nameDesc' },
    { value: 'rating-desc', by: 'Rating', dir: 'Descending', labelKey: 'recipes.list.sort.ratingDesc' },
    { value: 'rating-asc', by: 'Rating', dir: 'Ascending', labelKey: 'recipes.list.sort.ratingAsc' },
  ];

  private recipeApi = inject(RecipeApiService);
  private chatSearch = inject(RecipeChatSearchService);
  private destroyRef = inject(DestroyRef);
  private loader = inject(RecipeListLoaderService);
  private searchInput = signal('');

  constructor() {
    debouncedEffect(this.searchInput, UI.SEARCH_DEBOUNCE_MS, (value) => {
      const trimmed = value.trim();
      this.activeSearch.set(trimmed);
      if (trimmed === '' || !this.chatSearch.isConversational(trimmed)) {
        this.loader.setAiPowered(false);
        this.resetAndReload();
        return;
      }

      this.multiSelect.clearSelection();
      this.loader.loadFromChat(trimmed, {
        pageSize: this.pageSize(),
        sortBy: this.sortBy(),
        sortDirection: this.sortDirection(),
      });
    });
  }

  protected assignment = inject(RecipeAssignmentService);
  protected multiSelect = inject(MultiRecipeShoppingListService);

  recipes = this.loader.recipes;
  totalCount = this.loader.totalCount;
  currentPage = this.loader.currentPage;
  availableTags = this.loader.availableTags;
  isAiPowered = this.loader.isAiPowered;
  isLoading = this.loader.isLoading;
  pageSize = signal(UI.DEFAULT_PAGE_SIZE);
  sortBy = signal<'Name' | 'Date' | 'Rating'>('Date');
  sortDirection = signal<'Ascending' | 'Descending'>('Descending');
  serverError = computed(() => this.multiSelect.serverError() ?? this.loader.serverError());
  searchQuery = signal('');
  activeSearch = signal('');
  filter = signal<RecipeFilterValue>({ ...EMPTY_FILTER });
  filterPanelOpen = signal(false);

  // Pagination + load-more disabled in AI mode: chat returns a small fixed
  // set (top-N suggestions), there's no concept of "next page" to ask for.
  hasMore = computed(() => !this.isAiPowered() && this.recipes().length < this.totalCount());

  currentSort = computed(() => {
    const by = this.sortBy().toLowerCase();
    const dir = this.sortDirection() === 'Ascending' ? 'asc' : 'desc';
    return `${by}-${dir}`;
  });

  readonly sortOptions = RecipeListComponent.sortOptionList;

  filterActiveCount = computed(() => {
    const filter = this.filter();
    let count = filter.tags.length;
    if (filter.difficulty !== null) count += 1;
    if (filter.maxPrepTime !== null) count += 1;
    if (filter.maxCookTime !== null) count += 1;
    if (filter.favoritesOnly) count += 1;
    return count;
  });

  ngOnInit(): void {
    this.assignment.initFromRoute();
    this.multiSelect.initFromRoute();
    this.loader.load(this.buildParams(), false);
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.searchInput.set(value);
  }

  onSearchClear(): void {
    this.searchQuery.set('');
    this.activeSearch.set('');
    this.loader.setAiPowered(false);
    this.resetAndReload();
  }

  onSortSelect(value: string): void {
    const option = RecipeListComponent.sortOptionList.find((entry) => entry.value === value);
    if (!option) return;
    this.sortBy.set(option.by);
    this.sortDirection.set(option.dir);
    this.resetAndReload();
  }

  onLoadMore(): void {
    this.currentPage.update((page) => page + 1);
    this.loader.load(this.buildParams(), true);
  }

  toggleFilterPanel(): void {
    this.filterPanelOpen.update((open) => !open);
  }

  onToggleFavorite(identifier: string): void {
    toggleFavoriteInList(this.recipes, identifier, this.destroyRef, (id) => this.recipeApi.toggleFavorite(id));
  }

  onFilterChange(value: RecipeFilterValue): void {
    this.filter.set(value);
    this.resetAndReload();
  }

  private resetAndReload(): void {
    this.loader.reset();
    this.multiSelect.clearSelection();
    this.loader.load(this.buildParams(), false);
  }

  private buildParams(): GetRecipesParams {
    const search = this.activeSearch();
    const filter = this.filter();
    return {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection(),
      ...(search !== '' && { search }),
      ...(filter.tags.length > 0 && { tags: filter.tags }),
      ...(filter.difficulty !== null && { difficulty: filter.difficulty }),
      ...(filter.maxPrepTime !== null && { maxPrepTime: filter.maxPrepTime }),
      ...(filter.maxCookTime !== null && { maxCookTime: filter.maxCookTime }),
      ...(filter.favoritesOnly && { favorites: true }),
    };
  }
}
