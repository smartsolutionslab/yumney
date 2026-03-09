import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, ImportRecipeResponse } from '@yumney/shared/api-client';

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
  imports: [ReactiveFormsModule, TranslocoModule],
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
  serverError = signal<string | null>(null);
  extractedRecipe = signal<ImportRecipeResponse | null>(null);

  form = this.fb.nonNullable.group({
    url: ['', [Validators.required, Validators.maxLength(DashboardComponent.urlMaxLength), urlValidator]],
  });

  onImport(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.serverError.set(null);
    this.extractedRecipe.set(null);

    const { url } = this.form.getRawValue();

    this.recipeApi
      .importRecipe({ url })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.isLoading.set(false);
          this.extractedRecipe.set(response);
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

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!control?.hasError(error) && !!control?.touched;
  }
}
