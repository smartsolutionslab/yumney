import { Component, ChangeDetectionStrategy, computed, inject, OnInit, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import {
  RecipeApiService,
  RecipeListItem,
  GetRecipesParams,
} from '@yumney/shared/api-client';

@Component({
  selector: 'yn-recipe-list',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './recipe-list.component.html',
  styleUrl: './recipe-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeListComponent implements OnInit {
  private recipeApi = inject(RecipeApiService);
  private destroyRef = inject(DestroyRef);

  recipes = signal<RecipeListItem[]>([]);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(20);
  sortBy = signal<'Name' | 'Date'>('Date');
  sortDirection = signal<'Ascending' | 'Descending'>('Descending');
  isLoading = signal(false);
  error = signal<string | null>(null);

  hasMore = computed(() => this.recipes().length < this.totalCount());

  ngOnInit(): void {
    this.loadRecipes(false);
  }

  onSortChange(value: string): void {
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

  onLoadMore(): void {
    this.currentPage.update((p) => p + 1);
    this.loadRecipes(true);
  }

  private loadRecipes(append: boolean): void {
    this.isLoading.set(true);
    this.error.set(null);

    const params: GetRecipesParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection(),
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
        error: () => {
          this.isLoading.set(false);
          this.error.set('recipes.list.errors.generic');
        },
      });
  }
}
