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
import { RecipeApiService, RecipeListItem, GetRecipesParams } from '../api';
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
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);
  private searchInput = signal('');
  private loadRequestId = 0;

  constructor() {
    debouncedEffect(this.searchInput, UI.SEARCH_DEBOUNCE_MS, (value) => {
      this.activeSearch.set(value.trim());
      this.resetAndReload();
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
  filter = signal<RecipeFilterValue>({ ...EMPTY_FILTER });
  filterPanelOpen = signal(false);
  availableTags = signal<string[]>([]);

  hasMore = computed(() => this.recipes().length < this.totalCount());

  currentSort = computed(() => {
    const by = this.sortBy().toLowerCase();
    const dir = this.sortDirection() === 'Ascending' ? 'asc' : 'desc';
    return `${by}-${dir}`;
  });

  readonly sortOptions = RecipeListComponent.sortOptionList;

  filterActiveCount = computed(() => {
    const f = this.filter();
    let count = f.tags.length;
    if (f.difficulty !== null) count += 1;
    if (f.maxPrepTime !== null) count += 1;
    if (f.maxCookTime !== null) count += 1;
    if (f.favoritesOnly) count += 1;
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
    this.resetAndReload();
  }

  onSortSelect(value: string): void {
    const option = RecipeListComponent.sortOptionList.find((o) => o.value === value);
    if (!option) return;
    this.sortBy.set(option.by);
    this.sortDirection.set(option.dir);
    this.resetAndReload();
  }

  onLoadMore(): void {
    this.currentPage.update((p) => p + 1);
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

  private loadRecipes(append: boolean): void {
    const requestId = ++this.loadRequestId;
    const search = this.activeSearch();
    const f = this.filter();
    const params: GetRecipesParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection(),
      ...(search !== '' && { search }),
      ...(f.tags.length > 0 && { tags: f.tags }),
      ...(f.difficulty !== null && { difficulty: f.difficulty }),
      ...(f.maxPrepTime !== null && { maxPrepTime: f.maxPrepTime }),
      ...(f.maxCookTime !== null && { maxCookTime: f.maxCookTime }),
      ...(f.favoritesOnly && { favorites: true }),
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
