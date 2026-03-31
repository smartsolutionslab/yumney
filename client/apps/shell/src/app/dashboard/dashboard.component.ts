import {
  Component,
  ChangeDetectionStrategy,
  signal,
  inject,
  DestroyRef,
  OnInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import {
  RecipeApiService,
  ImportRecipeResponse,
  ImportStreamEvent,
} from '@yumney/shared/api-client';
import {
  urlValidator,
  createAsyncState,
  mapToSaveRecipeRequest,
  VALIDATION,
  HttpErrorMap,
} from '@yumney/shared/models';
import { RecipePreviewComponent, FormFieldComponent, SubmitButtonComponent } from '@yumney/ui';

@Component({
  selector: 'yn-dashboard',
  imports: [ReactiveFormsModule, TranslocoModule, RecipePreviewComponent, FormFieldComponent, SubmitButtonComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit {
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
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private recipeApi = inject(RecipeApiService);
  private importState = createAsyncState(inject(DestroyRef));
  private saveState = createAsyncState(inject(DestroyRef));

  private destroyRef = inject(DestroyRef);

  isLoading = this.importState.isLoading;
  isSaving = this.saveState.isLoading;
  serverError = signal<string | null>(null);
  extractedRecipe = signal<ImportRecipeResponse | null>(null);
  sourceUrl = signal<string | null>(null);
  saveSuccess = signal<string | null>(null);
  isManualEntry = signal(false);
  streamingStatus = signal<string | null>(null);
  streamingChunks = signal('');

  form = this.fb.nonNullable.group({
    url: ['', [Validators.required, Validators.maxLength(VALIDATION.URL_MAX_LENGTH), urlValidator]],
  });

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    const sharedUrl = params['url'] || this.extractUrl(params['text']);

    if (sharedUrl) {
      this.form.controls.url.setValue(sharedUrl);
      this.onImport();
    }
  }

  onImport(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.resetImportState();

    const { url } = this.form.getRawValue();

    this.importWithStreaming(url);
  }

  private importWithStreaming(url: string): void {
    this.importState.isLoading.set(true);

    this.recipeApi
      .importRecipeStream(url)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (event: ImportStreamEvent) => this.handleStreamEvent(event, url),
        error: () => {
          this.importState.isLoading.set(false);
          this.streamingStatus.set(null);
          this.serverError.set('dashboard.import.errors.generic');
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
        this.importState.isLoading.set(false);
        this.streamingStatus.set(null);
        try {
          const recipe = JSON.parse(event.data) as ImportRecipeResponse;
          this.extractedRecipe.set(recipe);
          this.sourceUrl.set(url);
          this.form.reset();
        } catch {
          this.serverError.set('dashboard.import.errors.generic');
        }
        break;
      case 'fail':
        this.importState.isLoading.set(false);
        this.streamingStatus.set(null);
        this.serverError.set('dashboard.import.errors.generic');
        break;
    }
  }

  onImportFromPhotos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) {
      return;
    }

    const photos = Array.from(files);
    input.value = '';

    this.resetImportState();

    this.importState.execute(
      this.recipeApi.importFromPhotos(photos),
      DashboardComponent.importErrorMap,
      (response) => {
        this.extractedRecipe.set(response);
        this.sourceUrl.set(null);
      },
      (error) => this.serverError.set(error),
    );
  }

  onCreateManually(): void {
    this.resetImportState();
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
    const request = mapToSaveRecipeRequest(recipe, this.sourceUrl() ?? undefined);

    this.serverError.set(null);
    this.saveSuccess.set(null);

    this.saveState.execute(
      this.recipeApi.saveRecipe(request),
      DashboardComponent.saveErrorMap,
      (saved) => {
        this.router.navigate(['/recipes', saved.identifier]);
      },
      (error) => this.serverError.set(error),
    );
  }

  onDiscardRecipe(): void {
    this.extractedRecipe.set(null);
    this.sourceUrl.set(null);
    this.isManualEntry.set(false);
    this.serverError.set(null);
  }

  private extractUrl(text: string | undefined): string | null {
    if (!text) {
      return null;
    }

    const match = text.match(/https?:\/\/\S+/i);
    return match ? match[0] : null;
  }

  private resetImportState(): void {
    this.serverError.set(null);
    this.extractedRecipe.set(null);
    this.saveSuccess.set(null);
    this.isManualEntry.set(false);
    this.streamingStatus.set(null);
    this.streamingChunks.set('');
  }
}
