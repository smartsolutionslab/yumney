import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';

@Component({
  selector: 'yn-recipe-detail',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailComponent implements OnInit {
  private recipeApi = inject(RecipeApiService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  recipe = signal<RecipeDetail | null>(null);
  isLoading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.error.set('recipes.detail.notFound');
      return;
    }

    this.isLoading.set(true);

    this.recipeApi
      .getRecipeById(identifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          this.recipe.set(recipe);
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          if (err.status === 404) {
            this.error.set('recipes.detail.notFound');
          } else {
            this.error.set('recipes.detail.errors.generic');
          }
        },
      });
  }

  totalTime(): number | null {
    const recipe = this.recipe();
    if (!recipe) {
      return null;
    }
    const prep = recipe.prepTimeMinutes ?? 0;
    const cook = recipe.cookTimeMinutes ?? 0;
    if (prep === 0 && cook === 0) {
      return null;
    }
    return prep + cook;
  }
}
