import { ChangeDetectionStrategy, Component, DestroyRef, OnDestroy, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { ImportRecipeResponse, RecipeApiService } from '@yumney/shared/api-recipes';
import type { RecognizedIngredient } from '@yumney/shared/api-recipes';
import { CameraService, ERROR_MAPS, UI, createAsyncState, mapToSaveRecipeRequest } from '@yumney/shared/models';
import {
  ButtonComponent,
  CameraCaptureComponent,
  IngredientScannerComponent,
  IngredientsToastComponent,
  MessageBannerComponent,
  RecipePreviewComponent,
} from '@yumney/ui';
import { UrlImportComponent } from '../url-import.component';

/**
 * Self-contained "import a recipe" widget — URL, photo, camera, scanner,
 * manual entry — composed inside the dashboard. Owns its own state so the
 * dashboard can stay focused on page-level concerns (suggestions, quick
 * actions, share-URL detection). Emits `recipeSaved` so the parent owns
 * the post-save navigation.
 */
@Component({
  selector: 'yn-import-panel',
  imports: [
    TranslocoModule,
    LucideAngularModule,
    UrlImportComponent,
    ButtonComponent,
    MessageBannerComponent,
    RecipePreviewComponent,
    CameraCaptureComponent,
    IngredientScannerComponent,
    IngredientsToastComponent,
  ],
  templateUrl: './import-panel.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportPanelComponent implements OnDestroy {
  /** When the dashboard receives an external share-intent URL, hand it down here. */
  readonly sharedUrl = input<string | null>(null);

  /** Emitted after a successful save so the parent can navigate to the saved recipe. */
  readonly recipeSaved = output<{ identifier: string; title: string }>();

  private readonly recipeApi = inject(RecipeApiService);
  protected readonly camera = inject(CameraService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly importState = createAsyncState();
  private readonly saveState = createAsyncState();
  private readonly urlImport = viewChild<UrlImportComponent>(UrlImportComponent);

  protected readonly importSectionExpanded = signal(false);
  readonly isLoading = this.importState.isLoading;
  readonly isSaving = this.saveState.isLoading;
  readonly serverError = signal<string | null>(null);
  readonly extractedRecipe = signal<ImportRecipeResponse | null>(null);
  readonly sourceUrl = signal<string | null>(null);
  readonly saveSuccess = signal<string | null>(null);
  readonly isManualEntry = signal(false);
  protected readonly cameraActive = signal(false);
  protected readonly scannerActive = signal(false);
  protected readonly recognizedIngredients = signal<RecognizedIngredient[] | null>(null);

  private navigateAfterSaveTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      const url = this.sharedUrl();
      if (!url) return;
      this.importSectionExpanded.set(true);
      // Defer one microtask so the @if (importSectionExpanded()) block renders
      // and the UrlImportComponent viewChild is available.
      queueMicrotask(() => this.urlImport()?.importUrl(url));
    });
  }

  /** Programmatic open used by the share-URL flow when the import section was collapsed. */
  expand(): void {
    this.importSectionExpanded.set(true);
  }

  ngOnDestroy(): void {
    this.cancelNavigateAfterSave();
  }

  protected toggleImportSection(): void {
    this.importSectionExpanded.update((open) => !open);
  }

  onUrlExtracted(payload: { recipe: ImportRecipeResponse; sourceUrl: string }): void {
    this.extractedRecipe.set(payload.recipe);
    this.sourceUrl.set(payload.sourceUrl);
  }

  onUrlImportFailed(errorKey: string): void {
    this.serverError.set(errorKey);
  }

  onUrlImportStarted(): void {
    this.resetImportState();
  }

  protected onImportFromPhotos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) return;

    input.value = '';
    this.importPhotos(Array.from(files));
  }

  protected onOpenCamera(): void {
    this.cameraActive.set(true);
  }

  protected onCameraCaptured(files: File[]): void {
    this.cameraActive.set(false);
    this.importPhotos(files);
  }

  protected onCameraCancelled(): void {
    this.cameraActive.set(false);
  }

  protected onCameraFallback(): void {
    this.cameraActive.set(false);
  }

  protected onOpenScanner(): void {
    this.scannerActive.set(true);
  }

  protected onIngredientsConfirmed(ingredients: RecognizedIngredient[]): void {
    this.scannerActive.set(false);
    this.recognizedIngredients.set(ingredients);
  }

  protected onScannerCancelled(): void {
    this.scannerActive.set(false);
  }

  protected dismissRecognizedIngredients(): void {
    this.recognizedIngredients.set(null);
  }

  private importPhotos(photos: File[]): void {
    this.resetImportState();

    this.importState.execute(
      this.recipeApi.importFromPhotos(photos),
      ERROR_MAPS.dashboard.import,
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
    this.cancelNavigateAfterSave();

    this.saveState.execute(
      this.recipeApi.saveRecipe(request),
      ERROR_MAPS.dashboard.save,
      (saved) => {
        // Show the success banner briefly so the user gets a confirmation
        // beat before the parent navigates. onImportAnother() cancels.
        this.saveSuccess.set(saved.title);
        this.navigateAfterSaveTimer = setTimeout(() => {
          this.navigateAfterSaveTimer = null;
          this.recipeSaved.emit({ identifier: saved.identifier, title: saved.title });
        }, UI.SAVED_INDICATOR_MS);
      },
      (error) => this.serverError.set(error),
    );
  }

  onDiscardRecipe(): void {
    this.cancelNavigateAfterSave();
    this.extractedRecipe.set(null);
    this.sourceUrl.set(null);
    this.isManualEntry.set(false);
    this.serverError.set(null);
    this.saveSuccess.set(null);
  }

  protected onImportAnother(): void {
    this.onDiscardRecipe();
    this.importSectionExpanded.set(true);
  }

  private cancelNavigateAfterSave(): void {
    if (this.navigateAfterSaveTimer !== null) {
      clearTimeout(this.navigateAfterSaveTimer);
      this.navigateAfterSaveTimer = null;
    }
  }

  private resetImportState(): void {
    this.serverError.set(null);
    this.extractedRecipe.set(null);
    this.saveSuccess.set(null);
    this.isManualEntry.set(false);
  }
}
