import {
  Component,
  ChangeDetectionStrategy,
  computed,
  effect,
  ElementRef,
  HostListener,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime } from 'rxjs';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeListItem, GetRecipesParams } from '@yumney/shared/api-client';
import { createAsyncState, ERROR_MAPS, ROUTES, UI } from '@yumney/shared/models';
import { RouterLink } from '@angular/router';
import {
  EMPTY_FILTER,
  FavoriteButtonComponent,
  FilterPanelComponent,
  InfiniteScrollDirective,
  prefersReducedMotion,
  type RecipeFilterValue,
  staggerFadeIn,
} from '@yumney/ui';

@Component({
  selector: 'yn-recipe-list',
  imports: [
    TranslocoModule,
    RouterLink,
    InfiniteScrollDirective,
    FilterPanelComponent,
    FavoriteButtonComponent,
  ],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private static readonly sortOptionList = [
    {
      value: 'date-desc',
      by: 'Date' as const,
      dir: 'Descending' as const,
      labelKey: 'recipes.list.sort.dateDesc',
    },
    {
      value: 'date-asc',
      by: 'Date' as const,
      dir: 'Ascending' as const,
      labelKey: 'recipes.list.sort.dateAsc',
    },
    {
      value: 'name-asc',
      by: 'Name' as const,
      dir: 'Ascending' as const,
      labelKey: 'recipes.list.sort.nameAsc',
    },
    {
      value: 'name-desc',
      by: 'Name' as const,
      dir: 'Descending' as const,
      labelKey: 'recipes.list.sort.nameDesc',
    },
  ];

  private recipeApi = inject(RecipeApiService);
  private destroyRef = inject(DestroyRef);
  private elementRef = inject(ElementRef);
  private asyncState = createAsyncState(this.destroyRef);
  private searchSubject = new Subject<string>();

  @HostListener('document:click', ['$event.target'])
  onDocumentClick(target: EventTarget | null): void {
    if (
      this.sortMenuOpen() &&
      target instanceof Node &&
      !this.elementRef.nativeElement.querySelector('.sort-dropdown')?.contains(target)
    ) {
      this.sortMenuOpen.set(false);
    }
  }

  private hostEl = inject(ElementRef);
  private previousCardCount = 0;

  constructor() {
    effect(() => {
      const count = this.recipes().length;
      if (count > this.previousCardCount && !prefersReducedMotion()) {
        requestAnimationFrame(() => {
          const cards = this.hostEl.nativeElement.querySelectorAll('.recipe-card');
          const newCards = Array.from(cards).slice(this.previousCardCount);
          if (newCards.length > 0) {
            staggerFadeIn(newCards as Element[]);
          }
          this.previousCardCount = count;
        });
      } else {
        this.previousCardCount = count;
      }
    });
  }

  recipes = signal<RecipeListItem[]>([]);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(UI.DEFAULT_PAGE_SIZE);
  sortBy = signal<'Name' | 'Date'>('Date');
  sortDirection = signal<'Ascending' | 'Descending'>('Descending');
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;
  searchQuery = signal('');
  activeSearch = signal('');
  filter = signal<RecipeFilterValue>({ ...EMPTY_FILTER });
  filterPanelOpen = signal(false);
  availableTags = signal<string[]>([]);

  hasMore = computed(() => this.recipes().length < this.totalCount());
  sortMenuOpen = signal(false);

  currentSort = computed(() => {
    const by = this.sortBy().toLowerCase();
    const dir = this.sortDirection() === 'Ascending' ? 'asc' : 'desc';
    return `${by}-${dir}`;
  });

  currentSortLabel = computed(
    () =>
      RecipeListComponent.sortOptionList.find((o) => o.value === this.currentSort())?.labelKey ??
      'recipes.list.sort.dateDesc',
  );

  readonly sortOptions = RecipeListComponent.sortOptionList;

  ngOnInit(): void {
    this.loadRecipes(false);

    this.searchSubject
      .pipe(debounceTime(UI.SEARCH_DEBOUNCE_MS), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.activeSearch.set(value.trim());
        this.currentPage.set(1);
        this.recipes.set([]);
        this.totalCount.set(0);
        this.loadRecipes(false);
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
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.loadRecipes(false);
  }

  toggleSortMenu(): void {
    this.sortMenuOpen.update((open) => !open);
  }

  closeSortMenu(): void {
    this.sortMenuOpen.set(false);
  }

  onSortSelect(value: string): void {
    const option = RecipeListComponent.sortOptionList.find((o) => o.value === value);
    if (!option) {
      return;
    }
    this.sortMenuOpen.set(false);
    this.sortBy.set(option.by);
    this.sortDirection.set(option.dir);
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.loadRecipes(false);
  }

  onLoadMore(): void {
    this.currentPage.update((p) => p + 1);
    this.loadRecipes(true);
  }

  toggleFilterPanel(): void {
    this.filterPanelOpen.update((open) => !open);
  }

  onToggleFavorite(identifier: string): void {
    // Optimistic update — flip immediately, revert on error.
    const current = this.recipes();
    const idx = current.findIndex((r) => r.identifier === identifier);
    if (idx === -1) return;
    const original = current[idx];
    this.recipes.update((list) => {
      const next = [...list];
      next[idx] = { ...original, isFavorite: !original.isFavorite };
      return next;
    });

    this.recipeApi
      .toggleFavorite(identifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (state) => {
          this.recipes.update((list) => {
            const j = list.findIndex((r) => r.identifier === identifier);
            if (j === -1) return list;
            const next = [...list];
            next[j] = { ...next[j], isFavorite: state.isFavorite };
            return next;
          });
        },
        error: () => {
          this.recipes.update((list) => {
            const j = list.findIndex((r) => r.identifier === identifier);
            if (j === -1) return list;
            const next = [...list];
            next[j] = { ...next[j], isFavorite: original.isFavorite };
            return next;
          });
        },
      });
  }

  onFilterChange(value: RecipeFilterValue): void {
    this.filter.set(value);
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.loadRecipes(false);
  }

  filterActiveCount = computed(() => {
    const f = this.filter();
    let count = f.tags.length;
    if (f.difficulty !== null) count += 1;
    if (f.maxPrepTime !== null) count += 1;
    if (f.maxCookTime !== null) count += 1;
    if (f.favoritesOnly) count += 1;
    return count;
  });

  private loadRecipes(append: boolean): void {
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
