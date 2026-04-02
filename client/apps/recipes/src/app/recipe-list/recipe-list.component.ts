import {
  AfterViewInit,
  Component,
  ChangeDetectionStrategy,
  computed,
  ElementRef,
  HostListener,
  inject,
  OnInit,
  DestroyRef,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime } from 'rxjs';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeListItem, GetRecipesParams } from '@yumney/shared/api-client';
import { createAsyncState, HttpErrorMap, UI } from '@yumney/shared/models';

@Component({
  selector: 'yn-recipe-list',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListComponent implements OnInit, AfterViewInit {
  private static readonly listErrorMap: HttpErrorMap = {
    default: 'recipes.list.errors.generic',
  };

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
  private observer: IntersectionObserver | null = null;
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

  scrollSentinel = viewChild<ElementRef>('scrollSentinel');

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

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting && this.hasMore() && !this.isLoading()) {
        this.loadMore();
      }
    });
    const sentinel = this.scrollSentinel()?.nativeElement;
    if (sentinel) {
      this.observer.observe(sentinel);
    }
    this.destroyRef.onDestroy(() => this.observer?.disconnect());
  }

  private loadMore(): void {
    this.currentPage.update((p) => p + 1);
    this.loadRecipes(true);
  }

  private loadRecipes(append: boolean): void {
    const search = this.activeSearch();
    const params: GetRecipesParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection(),
      ...(search !== '' && { search }),
    };

    this.asyncState.execute(
      this.recipeApi.getRecipes(params),
      RecipeListComponent.listErrorMap,
      (response) => {
        this.totalCount.set(response.totalCount);
        if (append) {
          this.recipes.update((existing) => [...existing, ...response.items]);
        } else {
          this.recipes.set(response.items);
        }
      },
    );
  }
}
