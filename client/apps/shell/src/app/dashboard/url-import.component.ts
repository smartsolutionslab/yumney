import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef, output, input } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, ImportRecipeResponse, ImportStreamEvent } from '@yumney/shared/api-recipes';
import { ERROR_MAPS, urlValidator, VALIDATION, ensureFormValid, mapHttpError } from '@yumney/shared/models';
import { FormFieldComponent, SubmitButtonComponent } from '@yumney/ui';

@Component({
  selector: 'yn-url-import',
  imports: [ReactiveFormsModule, TranslocoModule, FormFieldComponent, SubmitButtonComponent],
  templateUrl: './url-import.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UrlImportComponent {
  /** Disables submit while parent is processing a save. */
  isSaving = input(false);

  /** Emits when extraction completes — parent shows preview. */
  extracted = output<{ recipe: ImportRecipeResponse; sourceUrl: string }>();

  /** Emits when extraction fails — parent shows the error. */
  failed = output<string>();

  /** Emits when the user starts a new import — parent should clear stale state. */
  importStarted = output<void>();

  private formBuilder = inject(FormBuilder);
  private recipeApi = inject(RecipeApiService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(false);
  streamingStatus = signal<string | null>(null);
  streamingChunks = signal('');

  form = this.formBuilder.nonNullable.group({
    url: ['', [Validators.required, Validators.maxLength(VALIDATION.RECIPES.RECIPE_URL.MAX_LENGTH), urlValidator]],
  });

  /** Programmatic entry point for the share-target / external trigger flow. */
  importUrl(url: string): void {
    this.form.controls.url.setValue(url);
    this.onSubmit();
  }

  onSubmit(): void {
    if (!ensureFormValid(this.form)) return;

    this.importStarted.emit();
    this.streamingStatus.set(null);
    this.streamingChunks.set('');

    const { url } = this.form.getRawValue();
    this.isLoading.set(true);

    this.recipeApi
      .importRecipeStream(url)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (event: ImportStreamEvent) => this.handleStreamEvent(event, url),
        error: (err: unknown) => {
          this.isLoading.set(false);
          this.streamingStatus.set(null);
          this.failed.emit(
            err instanceof HttpErrorResponse ? mapHttpError(err, ERROR_MAPS.dashboard.import) : ERROR_MAPS.dashboard.import.default,
          );
        },
      });
  }

  private handleStreamEvent(event: ImportStreamEvent, url: string): void {
    switch (event.type) {
      case 'status':
        this.streamingStatus.set(event.data);
        break;
      case 'chunk':
        this.streamingChunks.update((prev) => prev + event.data);
        break;
      case 'done':
        this.isLoading.set(false);
        this.streamingStatus.set(null);
        try {
          const recipe = JSON.parse(event.data) as ImportRecipeResponse;
          this.form.reset();
          this.extracted.emit({ recipe, sourceUrl: url });
        } catch {
          this.failed.emit('dashboard.import.errors.generic');
        }
        break;
      case 'fail':
        this.isLoading.set(false);
        this.streamingStatus.set(null);
        this.failed.emit('dashboard.import.errors.generic');
        break;
    }
  }
}
