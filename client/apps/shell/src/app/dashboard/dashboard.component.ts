import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoModule } from '@jsverse/transloco';
import {
  RecipeApiService,
  ImportRecipeResponse,
  SaveRecipeRequest,
} from '@yumney/shared/api-client';
import { RecipePreviewComponent } from './recipe-preview/recipe-preview.component';

function urlValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;
  if (!value) {
    return null;
  }

  try {
    const url = new URL(value);
    if (url.protocol === 'http:' || url.protocol === 'https:') {
      return null;
    }
  } catch {
    // invalid URL
  }

  return { invalidUrl: true };
}

@Component({
  selector: 'yn-dashboard',
  imports: [ReactiveFormsModule, TranslocoModule, RecipePreviewComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private static readonly importErrorMap: Record<number, string> = {
    502: 'dashboard.import.errors.unreachable',
    504: 'dashboard.import.errors.timeout',
    404: 'dashboard.import.errors.noRecipe',
  };

  private static readonly defaultImportError = 'dashboard.import.errors.generic';
  private static readonly urlMaxLength = 2048;

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
    url: [
      '',
      [Validators.required, Validators.maxLength(DashboardComponent.urlMaxLength), urlValidator],
    ],
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
          this.serverError.set(
            DashboardComponent.importErrorMap[err.status] ?? DashboardComponent.defaultImportError,
          );
        },
      });
  }

  onCreateManually(): void {
    this.serverError.set(null);
    this.saveSuccess.set(null);
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
    const sourceUrl = this.sourceUrl();

    const request: SaveRecipeRequest = {
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
      sourceUrl: sourceUrl ?? undefined,
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
          if (err.status === 409) {
            this.serverError.set('dashboard.save.errors.duplicate');
          } else {
            this.serverError.set('dashboard.save.errors.generic');
          }
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
    const control = this.form.get(field);
    return !!control?.hasError(error) && !!control?.touched;
  }
}
