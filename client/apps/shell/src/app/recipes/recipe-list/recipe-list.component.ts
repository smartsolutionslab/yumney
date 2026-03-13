import {
  AfterViewInit,
  Component,
  ChangeDetectionStrategy,
  computed,
  ElementRef,
  inject,
  OnInit,
  DestroyRef,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeListItem, GetRecipesParams } from '@yumney/shared/api-client';
import { mapHttpError, HttpErrorMap } from '@yumney/shared/models';

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

  private recipeApi = inject(RecipeApiService);
  private destroyRef = inject(DestroyRef);
  private observer: IntersectionObserver | null = null;
  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  scrollSentinel = viewChild<ElementRef>('scrollSentinel');

  recipes = signal<RecipeListItem[]>([]);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(20);
  sortBy = signal<'Name' | 'Date'>('Date');
  sortDirection = signal<'Ascending' | 'Descending'>('Descending');
  isLoading = signal(false);
  serverError = signal<string | null>(null);
  searchQuery = signal('');
  activeSearch = signal('');

  hasMore = computed(() => this.recipes().length < this.totalCount());

  ngOnInit(): void {
    this.loadRecipes(false);
    this.destroyRef.onDestroy(() => {
      if (this.searchTimeout) {
        clearTimeout(this.searchTimeout);
      }
    });
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);

    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    this.searchTimeout = setTimeout(() => {
      this.activeSearch.set(value.trim());
      this.currentPage.set(1);
      this.recipes.set([]);
      this.totalCount.set(0);
      this.loadRecipes(false);
    }, 300);
  }

  onSearchClear(): void {
    this.searchQuery.set('');
    this.activeSearch.set('');
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.loadRecipes(false);
  }

  onSortChange(event: Event): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
      this.searchTimeout = null;
    }
    const value = (event.target as HTMLSelectElement).value;
    switch (value) {
      case 'date-desc':
        this.sortBy.set('Date');
        this.sortDirection.set('Descending');
        break;
      case 'date-asc':
        this.sortBy.set('Date');
        this.sortDirection.set('Ascending');
        break;
      case 'name-asc':
        this.sortBy.set('Name');
        this.sortDirection.set('Ascending');
        break;
      case 'name-desc':
        this.sortBy.set('Name');
        this.sortDirection.set('Descending');
        break;
    }
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.loadRecipes(false);
  }

  ngAfterViewInit(): void {
    this.observer = new IntersectionObserver((entries) => {
      if (entries[0].isIntersecting && this.hasMore() && !this.isLoading()) {
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
    this.isLoading.set(true);
    this.serverError.set(null);

    const search = this.activeSearch();
    const params: GetRecipesParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection(),
      ...(search !== '' && { search }),
    };

    this.recipeApi
      .getRecipes(params)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isLoading.set(false);
          this.totalCount.set(response.totalCount);
          if (append) {
            this.recipes.update((existing) => [...existing, ...response.items]);
          } else {
            this.recipes.set(response.items);
          }
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, RecipeListComponent.listErrorMap));
        },
      });
  }
}
