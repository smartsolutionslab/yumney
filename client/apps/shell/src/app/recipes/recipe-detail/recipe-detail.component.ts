import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { RecipeApiService, RecipeDetail } from '@yumney/shared/api-client';
import { mapHttpError, HttpErrorMap } from '@yumney/shared/models';

@Component({
  selector: 'yn-recipe-detail',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailComponent implements OnInit {
  private static readonly detailErrorMap: HttpErrorMap = {
    404: 'recipes.detail.notFound',
    default: 'recipes.detail.errors.generic',
  };

  private static readonly deleteErrorMap: HttpErrorMap = {
    404: 'recipes.detail.delete.errors.notFound',
    default: 'recipes.detail.delete.errors.generic',
  };

  private recipeApi = inject(RecipeApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private transloco = inject(TranslocoService);
  private destroyRef = inject(DestroyRef);

  recipe = signal<RecipeDetail | null>(null);
  isLoading = signal(false);
  isDeleting = signal(false);
  serverError = signal<string | null>(null);

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('recipes.detail.notFound');
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
          this.serverError.set(mapHttpError(err, RecipeDetailComponent.detailErrorMap));
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

  onDelete(): void {
    const recipe = this.recipe();
    if (!recipe) {
      return;
    }

    const message = this.transloco.translate('recipes.detail.delete.confirm', {
      title: recipe.title,
    });

    if (!confirm(message)) {
      return;
    }

    this.isDeleting.set(true);
    this.serverError.set(null);

    this.recipeApi
      .deleteRecipe(recipe.identifier)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isDeleting.set(false);
          this.router.navigate(['/recipes']);
        },
        error: (err: HttpErrorResponse) => {
          this.isDeleting.set(false);
          this.serverError.set(
            mapHttpError(err, RecipeDetailComponent.deleteErrorMap),
          );
        },
      });
  }
}
