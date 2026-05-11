import {
  Component,
  ChangeDetectionStrategy,
  computed,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import {
  RecipeApiService,
  RecipeListItem,
  GetRecipesParams,
  ChatApiService,
  type ChatRecipeSuggestion,
} from '../api';
import {
  createAsyncState,
  debouncedEffect,
  ERROR_MAPS,
  ROUTES,
  UI,
  toggleFavoriteInList,
} from '@yumney/shared/models';
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
import { RecipeAssignmentService } from './recipe-assignment.service';
import { MultiRecipePreviewDialogComponent } from './multi-recipe-preview-dialog/multi-recipe-preview-dialog.component';
import { MultiRecipeShoppingListService } from './multi-recipe-shopping-list.service';

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
  providers: [RecipeAssignmentService, MultiRecipeShoppingListService],
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
    {
      value: 'rating-desc',
      by: 'Rating',
      dir: 'Descending',
      labelKey: 'recipes.list.sort.ratingDesc',
    },
    {
      value: 'rating-asc',
      by: 'Rating',
      dir: 'Ascending',
      labelKey: 'recipes.list.sort.ratingAsc',
    },
  ];

  private recipeApi = inject(RecipeApiService);
  private chatApi = inject(ChatApiService);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);
  private searchInput = signal('');
  private loadRequestId = 0;

  constructor() {
    debouncedEffect(this.searchInput, UI.SEARCH_DEBOUNCE_MS, (value) => {
      const trimmed = value.trim();
      this.activeSearch.set(trimmed);
      if (trimmed === '' || !isConversationalQuery(trimmed)) {
        this.isAiPowered.set(false);
        this.resetAndReload();
        return;
      }

      this.searchViaChat(trimmed);
    });
  }

  protected assignment = inject(RecipeAssignmentService);
  protected multiSelect = inject(MultiRecipeShoppingListService);

  recipes = signal<RecipeListItem[]>([]);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(UI.DEFAULT_PAGE_SIZE);
  sortBy = signal<'Name' | 'Date' | 'Rating'>('Date');
  sortDirection = signal<'Ascending' | 'Descending'>('Descending');
  isLoading = this.asyncState.isLoading;
  serverError = computed(() => this.multiSelect.serverError() ?? this.asyncState.serverError());
  searchQuery = signal('');
  activeSearch = signal('');
  isAiPowered = signal(false);
  filter = signal<RecipeFilterValue>({ ...EMPTY_FILTER });
  filterPanelOpen = signal(false);
  availableTags = signal<string[]>([]);

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
    this.loadRecipes(false);
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.searchInput.set(value);
  }

  onSearchClear(): void {
    this.searchQuery.set('');
    this.activeSearch.set('');
    this.isAiPowered.set(false);
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
    this.loadRecipes(true);
  }

  toggleFilterPanel(): void {
    this.filterPanelOpen.update((open) => !open);
  }

  onToggleFavorite(identifier: string): void {
    toggleFavoriteInList(this.recipes, identifier, this.destroyRef, (id) =>
      this.recipeApi.toggleFavorite(id),
    );
  }

  onFilterChange(value: RecipeFilterValue): void {
    this.filter.set(value);
    this.resetAndReload();
  }

  private resetAndReload(): void {
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.multiSelect.clearSelection();
    this.loadRecipes(false);
  }

  // Conversational query path: send the query through the chat service
  // (which calls the search_recipes SK tool internally) and hydrate each
  // returned suggestion's identifier into a full RecipeListItem for the
  // grid. Falls back to keyword search if the chat call fails — a transient
  // LLM outage must not regress baseline search.
  private searchViaChat(query: string): void {
    const requestId = ++this.loadRequestId;
    this.isAiPowered.set(true);
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.multiSelect.clearSelection();

    this.asyncState.execute(
      this.chatApi.send({ message: query, history: [] }).pipe(
        switchMap((response) => this.hydrateSuggestions(response.suggestions)),
        catchError(() => {
          this.isAiPowered.set(false);
          return this.recipeApi
            .getRecipes({
              page: 1,
              pageSize: this.pageSize(),
              sortBy: this.sortBy(),
              sortDirection: this.sortDirection(),
              search: query,
            })
            .pipe(map((response) => response.items));
        }),
      ),
      ERROR_MAPS.recipes.list,
      (items) => {
        if (requestId !== this.loadRequestId) return;
        this.recipes.set(items);
        this.totalCount.set(items.length);
        this.collectAvailableTags(items);
      },
    );
  }

  private hydrateSuggestions(suggestions: ChatRecipeSuggestion[]) {
    const validIds = suggestions
      .map((suggestion) => suggestion.recipeIdentifier)
      .filter((id): id is string => id !== null);

    if (validIds.length === 0) return of([] as RecipeListItem[]);

    return forkJoin(
      validIds.map((id) =>
        this.recipeApi.getRecipeById(id).pipe(
          map(
            (detail): RecipeListItem => ({
              identifier: detail.identifier,
              title: detail.title,
              description: detail.description,
              servings: detail.servings,
              prepTimeMinutes: detail.prepTimeMinutes,
              cookTimeMinutes: detail.cookTimeMinutes,
              difficulty: detail.difficulty,
              imageUrl: detail.imageUrl,
              createdAt: detail.createdAt,
              tags: detail.tags,
              isFavorite: detail.isFavorite,
              rating: detail.rating,
              hasNotes: detail.notes !== null,
            }),
          ),
          // Per-suggestion failure shouldn't tank the AI search — skip the
          // missing one and surface the rest.
          catchError(() => of(null)),
        ),
      ),
    ).pipe(map((items) => items.filter((item): item is RecipeListItem => item !== null)));
  }

  private loadRecipes(append: boolean): void {
    const requestId = ++this.loadRequestId;
    const search = this.activeSearch();
    const filter = this.filter();
    const params: GetRecipesParams = {
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

    this.asyncState.execute(
      this.recipeApi.getRecipes(params),
      ERROR_MAPS.recipes.list,
      (response) => {
        if (requestId !== this.loadRequestId) return;
        this.totalCount.set(response.totalCount);
        if (append) {
          this.recipes.update((existing) => [...existing, ...response.items]);
        } else {
          this.recipes.set(response.items);
        }
        this.collectAvailableTags(response.items);
      },
    );
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

// Conversational-query detector. Triggers the chat path when the user
// types something that looks like an intent rather than a keyword: more
// than 3 words, more than 20 chars, or contains a question/intent phrase.
// Single-word queries like "Bolognese" stay on the keyword path.
function isConversationalQuery(value: string): boolean {
  const wordCount = value.split(/\s+/).filter((word) => word.length > 0).length;
  if (wordCount > 3) return true;
  if (value.length > 20) return true;
  const lower = value.toLowerCase();
  const intentPhrases = ['what', 'how', 'show me', 'find me', 'i want', 'something', 'with'];
  return intentPhrases.some((phrase) => lower.includes(phrase));
}
