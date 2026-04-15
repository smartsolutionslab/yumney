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
  viewChild,
  viewChildren,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, debounceTime } from 'rxjs';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { RecipeApiService, RecipeListItem, GetRecipesParams } from '@yumney/shared/api-client';
import {
  createAsyncState,
  ERROR_MAPS,
  ROUTES,
  UI,
  toggleFavoriteInList,
} from '@yumney/shared/models';
import { RouterLink } from '@angular/router';
import {
  EMPTY_FILTER,
  FilterPanelComponent,
  InfiniteScrollDirective,
  prefersReducedMotion,
  type RecipeFilterValue,
  staggerFadeIn,
} from '@yumney/ui';
import { RecipeCardComponent } from './recipe-card/recipe-card.component';

@Component({
  selector: 'yn-recipe-list',
  imports: [
    TranslocoModule,
    RouterLink,
    LucideAngularModule,
    InfiniteScrollDirective,
    FilterPanelComponent,
    RecipeCardComponent,
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
  private asyncState = createAsyncState(this.destroyRef);
  private searchSubject = new Subject<string>();
  private loadRequestId = 0;

  private sortDropdown = viewChild<ElementRef>('sortDropdown');
  private recipeCards = viewChildren<ElementRef>('recipeCard');

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.sortMenuOpen()) {
      this.sortMenuOpen.set(false);
    }
  }

  @HostListener('document:click', ['$event.target'])
  onDocumentClick(target: EventTarget | null): void {
    const dropdown = this.sortDropdown()?.nativeElement;
    if (this.sortMenuOpen() && target instanceof Node && !dropdown?.contains(target)) {
      this.sortMenuOpen.set(false);
    }
  }

  private previousCardCount = 0;

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
    toggleFavoriteInList(this.recipes, identifier, this.destroyRef, (id) =>
      this.recipeApi.toggleFavorite(id),
    );
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
