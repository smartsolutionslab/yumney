import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, ImportRecipeResponse } from '@yumney/shared/api-client';
import { createAsyncState, mapToUpdateRecipeRequest, mapDetailToImportResponse, HttpErrorMap, ROUTES } from '@yumney/shared/models';
import { BackLinkComponent, LoadingSpinnerComponent, RecipePreviewComponent } from '@yumney/ui';

@Component({
  selector: 'yn-recipe-edit',
  imports: [TranslocoModule, BackLinkComponent, LoadingSpinnerComponent, RecipePreviewComponent],
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
  private loadState = createAsyncState(inject(DestroyRef));
  private saveState = createAsyncState(inject(DestroyRef));

  recipeData = signal<ImportRecipeResponse | null>(null);
  isLoading = this.loadState.isLoading;
  isSaving = this.saveState.isLoading;
  serverError = signal<string | null>(null);

  identifier = signal('');

  ngOnInit(): void {
    this.identifier.set(this.route.snapshot.paramMap.get('identifier') ?? '');
    if (!this.identifier()) {
      this.serverError.set('recipes.edit.errors.notFound');
      return;
    }

    this.loadState.execute(
      this.recipeApi.getRecipeById(this.identifier()),
      RecipeEditComponent.errorMap,
      (recipe) => this.recipeData.set(mapDetailToImportResponse(recipe)),
      (error) => this.serverError.set(error),
    );
  }

  onSave(recipe: ImportRecipeResponse): void {
    const request = mapToUpdateRecipeRequest(recipe);

    this.serverError.set(null);

    this.saveState.execute(
      this.recipeApi.updateRecipe(this.identifier(), request),
      RecipeEditComponent.errorMap,
      () => this.router.navigate([ROUTES.recipes.list, this.identifier()]),
      (error) => this.serverError.set(error),
    );
  }

  onDiscard(): void {
    this.router.navigate([ROUTES.recipes.list, this.identifier()]);
  }
}
