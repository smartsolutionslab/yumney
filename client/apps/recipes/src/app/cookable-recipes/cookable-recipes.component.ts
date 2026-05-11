import {
  Component,
  ChangeDetectionStrategy,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  RecipeApiService,
  type CookableRecipeListItem,
  type GetCookableRecipesParams,
} from '../api';
import { createAsyncState, ERROR_MAPS, ROUTES, UI } from '@yumney/shared/models';
import {
  ButtonComponent,
  EmptyStateComponent,
  InfiniteScrollDirective,
  MessageBannerComponent,
  StaggerNewItemsDirective,
} from '@yumney/ui';

@Component({
  selector: 'yn-cookable-recipes',
  imports: [
    TranslocoModule,
    RouterLink,
    LucideAngularModule,
    InfiniteScrollDirective,
    StaggerNewItemsDirective,
    ButtonComponent,
    EmptyStateComponent,
    MessageBannerComponent,
  ],
  templateUrl: './cookable-recipes.component.html',
  styleUrl: './cookable-recipes.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CookableRecipesComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private recipeApi = inject(RecipeApiService);
  private destroyRef = inject(DestroyRef);
  private asyncState = createAsyncState(this.destroyRef);
  private loadRequestId = 0;

  recipes = signal<CookableRecipeListItem[]>([]);
  totalCount = signal(0);
  currentPage = signal(1);
  pageSize = signal(UI.DEFAULT_PAGE_SIZE);
  fullMatchOnly = signal(false);
  isLoading = this.asyncState.isLoading;
  serverError = this.asyncState.serverError;

  hasMore = computed(() => this.recipes().length < this.totalCount());

  ngOnInit(): void {
    this.load(false);
  }

  onToggleFullMatchOnly(): void {
    this.fullMatchOnly.update((value) => !value);
    this.currentPage.set(1);
    this.recipes.set([]);
    this.totalCount.set(0);
    this.load(false);
  }

  onLoadMore(): void {
    this.currentPage.update((page) => page + 1);
    this.load(true);
  }

  protected tierKey(item: CookableRecipeListItem): string {
    return item.tier === 'Full' ? 'recipes.cookable.tier.full' : 'recipes.cookable.tier.near';
  }

  private load(append: boolean): void {
    const requestId = ++this.loadRequestId;
    const params: GetCookableRecipesParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      ...(this.fullMatchOnly() && { fullMatchOnly: true }),
    };

    this.asyncState.execute(
      this.recipeApi.getCookableRecipes(params),
      ERROR_MAPS.recipes.cookable,
      (response) => {
        if (requestId !== this.loadRequestId) return;
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
