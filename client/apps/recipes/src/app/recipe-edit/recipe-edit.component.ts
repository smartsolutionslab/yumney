import {
  Component,
  ChangeDetectionStrategy,
  computed,
  inject,
  OnInit,
  DestroyRef,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, ImportRecipeResponse } from '../api';
import {
  createAsyncState,
  mapToUpdateRecipeRequest,
  mapDetailToImportResponse,
  ERROR_MAPS,
  ROUTES,
} from '@yumney/shared/models';
import {
  BackLinkComponent,
  ConfirmDialogComponent,
  LoadingSpinnerComponent,
  RecipePreviewComponent,
} from '@yumney/ui';

@Component({
  selector: 'yn-recipe-edit',
  imports: [
    TranslocoModule,
    BackLinkComponent,
    ConfirmDialogComponent,
    LoadingSpinnerComponent,
    RecipePreviewComponent,
  ],
  templateUrl: './recipe-edit.component.html',
  styleUrl: './recipe-edit.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeEditComponent implements OnInit {
  private recipeApi = inject(RecipeApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private loadState = createAsyncState(inject(DestroyRef));
  private saveState = createAsyncState(inject(DestroyRef));

  recipeData = signal<ImportRecipeResponse | null>(null);
  isLoading = this.loadState.isLoading;
  isSaving = this.saveState.isLoading;
  showDiscardConfirm = signal(false);

  private notFoundError = signal<string | null>(null);
  serverError = computed(
    () => this.notFoundError() ?? this.loadState.serverError() ?? this.saveState.serverError(),
  );

  identifier = signal('');

  ngOnInit(): void {
    this.identifier.set(this.route.snapshot.paramMap.get('identifier') ?? '');
    if (!this.identifier()) {
      this.notFoundError.set('recipes.edit.errors.notFound');
      return;
    }

    this.loadState.execute(
      this.recipeApi.getRecipeById(this.identifier()),
      ERROR_MAPS.recipes.edit,
      (recipe) => this.recipeData.set(mapDetailToImportResponse(recipe)),
    );
  }

  onSave(recipe: ImportRecipeResponse): void {
    const request = mapToUpdateRecipeRequest(recipe);

    this.saveState.execute(
      this.recipeApi.updateRecipe(this.identifier(), request),
      ERROR_MAPS.recipes.edit,
      () => this.router.navigate([ROUTES.recipes.list, this.identifier()]),
    );
  }

  onDiscard(): void {
    if (this.isSaving()) return;
    this.showDiscardConfirm.set(true);
  }

  onDiscardConfirmed(): void {
    this.showDiscardConfirm.set(false);
    this.router.navigate([ROUTES.recipes.list, this.identifier()]);
  }

  onDiscardCancelled(): void {
    this.showDiscardConfirm.set(false);
  }
}
