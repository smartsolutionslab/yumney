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
import { TranslocoModule } from '@jsverse/transloco';
import {
  RecipeApiService,
  ImportRecipeResponse,
  UpdateRecipeRequest,
} from '@yumney/shared/api-client';
import { mapHttpError, HttpErrorMap } from '@yumney/shared/models';
import { RecipePreviewComponent } from '@yumney/ui';

@Component({
  selector: 'yn-recipe-edit',
  imports: [TranslocoModule, RouterLink, RecipePreviewComponent],
  templateUrl: './recipe-edit.component.html',
  styleUrl: './recipe-edit.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeEditComponent implements OnInit {
  private static readonly errorMap: HttpErrorMap = {
    404: 'recipes.edit.errors.notFound',
    default: 'recipes.edit.errors.generic',
  };

  private recipeApi = inject(RecipeApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  recipeData = signal<ImportRecipeResponse | null>(null);
  isLoading = signal(false);
  isSaving = signal(false);
  serverError = signal<string | null>(null);

  identifier = signal('');

  ngOnInit(): void {
    this.identifier.set(this.route.snapshot.paramMap.get('identifier') ?? '');
    if (!this.identifier()) {
      this.serverError.set('recipes.edit.errors.notFound');
      return;
    }

    this.isLoading.set(true);

    this.recipeApi
      .getRecipeById(this.identifier())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          this.recipeData.set({
            title: recipe.title,
            description: recipe.description,
            ingredients: recipe.ingredients,
            steps: recipe.steps,
            servings: recipe.servings,
            prepTimeMinutes: recipe.prepTimeMinutes,
            cookTimeMinutes: recipe.cookTimeMinutes,
            difficulty: recipe.difficulty,
            imageUrl: recipe.imageUrl,
          });
          this.isLoading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, RecipeEditComponent.errorMap));
        },
      });
  }

  onSave(recipe: ImportRecipeResponse): void {
    const request: UpdateRecipeRequest = {
      title: recipe.title,
      description: recipe.description,
      ingredients: recipe.ingredients.map((i) => ({
        name: i.name,
        amount: i.amount,
        unit: i.unit,
      })),
      steps: recipe.steps.map((s) => ({
        number: s.number,
        description: s.description,
      })),
      servings: recipe.servings,
      prepTimeMinutes: recipe.prepTimeMinutes,
      cookTimeMinutes: recipe.cookTimeMinutes,
      difficulty: recipe.difficulty,
      imageUrl: recipe.imageUrl,
    };

    this.isSaving.set(true);
    this.serverError.set(null);

    this.recipeApi
      .updateRecipe(this.identifier(), request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.router.navigate(['/recipes', this.identifier()]);
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving.set(false);
          this.serverError.set(mapHttpError(err, RecipeEditComponent.errorMap));
        },
      });
  }

  onDiscard(): void {
    this.router.navigate(['/recipes', this.identifier()]);
  }
}
