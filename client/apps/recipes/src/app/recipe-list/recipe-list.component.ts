import {
  Component,
  ChangeDetectionStrategy,
  computed,
  effect,
  ElementRef,
  inject,
  OnInit,
  DestroyRef,
  signal,
  viewChildren,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime } from 'rxjs';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { RecipeApiService, RecipeListItem, GetRecipesParams, ShoppingApiService } from '../api';
import {
  createAsyncState,
  ERROR_MAPS,
  ROUTES,
  UI,
  toggleFavoriteInList,
} from '@yumney/shared/models';
import { Router, RouterLink } from '@angular/router';
import {
  EMPTY_FILTER,
  FilterPanelComponent,
  InfiniteScrollDirective,
  prefersReducedMotion,
  type RecipeFilterValue,
  staggerFadeIn,
} from '@yumney/ui';
import { RecipeCardComponent } from './recipe-card/recipe-card.component';
import { SortMenuComponent, SortMenuOption } from './sort-menu/sort-menu.component';
import { RecipeAssignmentService } from './recipe-assignment.service';

interface SortOption extends SortMenuOption {
  by: 'Name' | 'Date';
  dir: 'Ascending' | 'Descending';
}

@Component({
  selector: 'yn-recipe-list',
  imports: [
    TranslocoModule,
    RouterLink,
    LucideAngularModule,
    InfiniteScrollDirective,
    FilterPanelComponent,
    RecipeCardComponent,
    SortMenuComponent,
  ],
  providers: [RecipeAssignmentService],
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
  ];

  private recipeApi = inject(RecipeApiService);
  private shoppingApi = inject(ShoppingApiService);
  private router = inject(Router);
  private transloco = inject(TranslocoService);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);
  private createShoppingListState = createAsyncState(this.destroyRef);
  private searchSubject = new Subject<string>();
  private loadRequestId = 0;
  private previousCardCount = 0;
  private recipeCards = viewChildren<ElementRef>('recipeCard');

  protected assignment = inject(RecipeAssignmentService);

  recipes = signal<RecipeListItem[]>([]);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(UI.DEFAULT_PAGE_SIZE);
  sortBy = signal<'Name' | 'Date'>('Date');
  sortDirection = signal<'Ascending' | 'Descending'>('Descending');
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;
  isCreatingMultiShoppingList = this.createShoppingListState.isLoading;
  searchQuery = signal('');
  activeSearch = signal('');
  filter = signal<RecipeFilterValue>({ ...EMPTY_FILTER });
  filterPanelOpen = signal(false);
  availableTags = signal<string[]>([]);
  multiSelectMode = signal(false);
  selectedRecipeIds = signal<ReadonlySet<string>>(new Set<string>());

  selectedCount = computed(() => this.selectedRecipeIds().size);
  hasSelection = computed(() => this.selectedCount() > 0);
  isRecipeSelected = (identifier: string): boolean => this.selectedRecipeIds().has(identifier);

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

  constructor() {
    effect(() => {
      const count = this.recipes().length;
      if (count > this.previousCardCount && !prefersReducedMotion()) {
        const prev = this.previousCardCount;
        this.previousCardCount = count;
        requestAnimationFrame(() => {
          const cards = this.recipeCards();
          const newCards = cards.slice(prev).map((ref) => ref.nativeElement);
          if (newCards.length > 0) {
            staggerFadeIn(newCards as Element[]);
          }
        });
      } else {
        this.previousCardCount = count;
      }
    });
  }

  ngOnInit(): void {
    this.assignment.initFromRoute();
    this.loadRecipes(false);

    this.searchSubject
      .pipe(debounceTime(UI.SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.activeSearch.set(value.trim());
        this.resetAndReload();
      });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.searchSubject.next(value);
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

  onToggleMultiSelectMode(): void {
    const next = !this.multiSelectMode();
    this.multiSelectMode.set(next);
    if (!next) {
      this.selectedRecipeIds.set(new Set<string>());
    }
  }

  onToggleRecipeSelection(identifier: string): void {
    const next = new Set(this.selectedRecipeIds());
    if (next.has(identifier)) {
      next.delete(identifier);
    } else {
      next.add(identifier);
    }
    this.selectedRecipeIds.set(next);
  }

  onCreateMultiShoppingList(): void {
    if (!this.hasSelection()) return;

    const ids = [...this.selectedRecipeIds()];
    const recipesByIdentifier = new Map(
      this.recipes().map((recipe) => [recipe.identifier, recipe]),
    );
    const title = this.transloco.translate('recipes.list.multiSelect.autoTitle', {
      count: ids.length,
    });
    const recipes = ids.map((identifier) => ({
      recipeIdentifier: identifier,
      servings: recipesByIdentifier.get(identifier)?.servings ?? null,
    }));

    this.createShoppingListState.execute(
      this.shoppingApi.createShoppingListFromRecipes({ title, recipes }),
      ERROR_MAPS.recipes.createShoppingList,
      (created) => {
        this.multiSelectMode.set(false);
        this.selectedRecipeIds.set(new Set<string>());
        this.router.navigateByUrl(ROUTES.shopping.detail(created.identifier));
      },
    );
  }

  private resetAndReload(): void {
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
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
