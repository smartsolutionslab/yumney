import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoModule } from '@jsverse/transloco';
import {
  RecipeApiService,
  ImportRecipeResponse,
  SaveRecipeRequest,
} from '@yumney/shared/api-client';
import {
  urlValidator,
  hasControlError,
  mapHttpError,
  VALIDATION,
  HttpErrorMap,
} from '@yumney/shared/models';
import { RecipePreviewComponent } from '@yumney/ui';

@Component({
  selector: 'yn-dashboard',
  imports: [ReactiveFormsModule, TranslocoModule, RecipePreviewComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private static readonly importErrorMap: HttpErrorMap = {
    502: 'dashboard.import.errors.unreachable',
    504: 'dashboard.import.errors.timeout',
    404: 'dashboard.import.errors.noRecipe',
    default: 'dashboard.import.errors.generic',
  };

  private static readonly saveErrorMap: HttpErrorMap = {
    409: 'dashboard.save.errors.duplicate',
    default: 'dashboard.save.errors.generic',
  };

  private fb = inject(FormBuilder);
  private recipeApi = inject(RecipeApiService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(false);
  isSaving = signal(false);
  serverError = signal<string | null>(null);
  extractedRecipe = signal<ImportRecipeResponse | null>(null);
  sourceUrl = signal<string | null>(null);
  saveSuccess = signal<string | null>(null);
  isManualEntry = signal(false);

  form = this.fb.nonNullable.group({
    url: ['', [Validators.required, Validators.maxLength(VALIDATION.URL_MAX_LENGTH), urlValidator]],
  });

  onImport(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.serverError.set(null);
    this.extractedRecipe.set(null);
    this.saveSuccess.set(null);
    this.isManualEntry.set(false);

    const { url } = this.form.getRawValue();

    this.recipeApi
      .importRecipe({ url })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isLoading.set(false);
          this.extractedRecipe.set(response);
          this.sourceUrl.set(url);
          this.form.reset();
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, DashboardComponent.importErrorMap));
        },
      });
  }

  onCreateManually(): void {
    this.serverError.set(null);
    this.saveSuccess.set(null);
    this.sourceUrl.set(null);
    this.isManualEntry.set(true);
    this.extractedRecipe.set({
      title: '',
      description: null,
      ingredients: [{ name: '', amount: null, unit: null }],
      steps: [{ number: 1, description: '' }],
      servings: null,
      prepTimeMinutes: null,
      cookTimeMinutes: null,
      difficulty: null,
      imageUrl: null,
    });
  }

  onSaveRecipe(recipe: ImportRecipeResponse): void {
    const {
      title,
      description,
      ingredients,
      steps,
      servings,
      prepTimeMinutes,
      cookTimeMinutes,
      difficulty,
      imageUrl,
    } = recipe;

    const request: SaveRecipeRequest = {
      title,
      description,
      ingredients: ingredients.map(({ name, amount, unit }) => ({ name, amount, unit })),
      steps: steps.map(({ number, description }) => ({ number, description })),
      servings,
      prepTimeMinutes,
      cookTimeMinutes,
      difficulty,
      imageUrl,
      sourceUrl: this.sourceUrl() ?? undefined,
    };

    this.isSaving.set(true);
    this.serverError.set(null);
    this.saveSuccess.set(null);

    this.recipeApi
      .saveRecipe(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (saved) => {
          this.isSaving.set(false);
          this.extractedRecipe.set(null);
          this.isManualEntry.set(false);
          this.saveSuccess.set(saved.title);
        },
        error: (err: HttpErrorResponse) => {
          this.isSaving.set(false);
          this.serverError.set(mapHttpError(err, DashboardComponent.saveErrorMap));
        },
      });
  }

  onDiscardRecipe(): void {
    this.extractedRecipe.set(null);
    this.sourceUrl.set(null);
    this.isManualEntry.set(false);
    this.serverError.set(null);
  }

  hasError(field: string, error: string): boolean {
    return hasControlError(this.form, field, error);
  }
}
