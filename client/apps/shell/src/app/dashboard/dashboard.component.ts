import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef, OnInit, effect, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { RecipeApiService, ImportRecipeResponse } from '@yumney/shared/api-recipes';
import { createAsyncState, mapToSaveRecipeRequest, ERROR_MAPS, ROUTES, UI } from '@yumney/shared/models';
import { LucideAngularModule } from 'lucide-angular';
import {
  ButtonComponent,
  CameraCaptureComponent,
  IngredientScannerComponent,
  IngredientsToastComponent,
  MessageBannerComponent,
  QuickActionsComponent,
  RecentActivityComponent,
  RecipePreviewComponent,
  ShareToastComponent,
  SuggestionCardComponent,
} from '@yumney/ui';
import { CameraService } from '@yumney/shared/models';
import type { RecognizedIngredient } from '@yumney/shared/api-recipes';
import { UrlImportComponent } from '../integrations/recipes/url-import.component';
import { DashboardSuggestionsService } from './dashboard-suggestions.service';

@Component({
  selector: 'yn-dashboard',
  imports: [
    TranslocoModule,
    LucideAngularModule,
    RouterLink,
    UrlImportComponent,
    ButtonComponent,
    MessageBannerComponent,
    RecipePreviewComponent,
    QuickActionsComponent,
    SuggestionCardComponent,
    RecentActivityComponent,
    CameraCaptureComponent,
    IngredientScannerComponent,
    ShareToastComponent,
    IngredientsToastComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardSuggestionsService],
})
export class DashboardComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private recipeApi = inject(RecipeApiService);
  protected camera = inject(CameraService);
  private destroyRef = inject(DestroyRef);
  private importState = createAsyncState();
  private saveState = createAsyncState();
  private suggestionsState = inject(DashboardSuggestionsService);
  private navigateAfterSaveTimer: ReturnType<typeof setTimeout> | null = null;

  protected urlImport = viewChild<UrlImportComponent>(UrlImportComponent);

  isLoading = this.importState.isLoading;
  isSaving = this.saveState.isLoading;
  serverError = signal<string | null>(null);
  extractedRecipe = signal<ImportRecipeResponse | null>(null);
  sourceUrl = signal<string | null>(null);
  saveSuccess = signal<string | null>(null);
  isManualEntry = signal(false);
  importSectionExpanded = signal(false);
  cameraActive = signal(false);
  scannerActive = signal(false);
  shareToast = signal<string | null>(null);
  recognizedIngredients = signal<RecognizedIngredient[] | null>(null);

  // Re-exposed so the template (and existing tests) keep their flat access.
  quickActions = this.suggestionsState.quickActions;
  suggestions = this.suggestionsState.suggestions;
  recentActivity = this.suggestionsState.recentActivity;
  suggestionsLoading = this.suggestionsState.loading;

  private readonly queryParams = toSignal(this.route.queryParams, {
    initialValue: {} as Record<string, string>,
  });

  constructor() {
    effect(() => {
      const params = this.queryParams();
      const urlParam = params['url'] as string | undefined;
      const textParam = params['text'] as string | undefined;
      const sharedText = urlParam || textParam;

      if (!sharedText) return;

      const sharedUrl = urlParam || this.extractUrl(textParam);

      if (sharedUrl) {
        this.handleSharedUrl(sharedUrl);
      } else if (textParam) {
        this.serverError.set('dashboard.share.noUrlFound');
        this.importSectionExpanded.set(true);
      }
    });
  }

  ngOnInit(): void {
    this.suggestionsState
      .load()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ initialDataIsEmpty }) => {
        if (initialDataIsEmpty) this.importSectionExpanded.set(true);
      });
    this.destroyRef.onDestroy(() => this.cancelNavigateAfterSave());
  }

  private handleSharedUrl(sharedUrl: string): void {
    this.importSectionExpanded.set(true);
    this.shareToast.set(sharedUrl);
    // Defer one microtask so the *@if (importSectionExpanded())* block renders
    // and the UrlImportComponent ViewChild is available.
    queueMicrotask(() => this.urlImport()?.importUrl(sharedUrl));
  }

  dismissShareToast(): void {
    this.shareToast.set(null);
  }

  toggleImportSection(): void {
    this.importSectionExpanded.update((open) => !open);
  }

  onQuickAction(actionKey: string): void {
    if (actionKey === 'cook_now') {
      void this.router.navigate([ROUTES.recipes.list]);
      return;
    }
    if (actionKey === 'meal_prep') {
      const ids = (this.suggestions()?.suggestions ?? []).map((item) => item.recipeIdentifier);
      const queryParams: Record<string, string> = { multiSelect: 'true' };
      if (ids.length > 0) queryParams['preselect'] = ids.join(',');
      void this.router.navigate([ROUTES.recipes.list], { queryParams });
      return;
    }
    this.importSectionExpanded.set(true);
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

  onImportFromPhotos(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (!files || files.length === 0) return;

    input.value = '';
    this.importPhotos(Array.from(files));
  }

  onOpenCamera(): void {
    this.cameraActive.set(true);
  }

  onCameraCaptured(files: File[]): void {
    this.cameraActive.set(false);
    this.importPhotos(files);
  }

  onCameraCancelled(): void {
    this.cameraActive.set(false);
  }

  onCameraFallback(): void {
    this.cameraActive.set(false);
  }

  onOpenScanner(): void {
    this.scannerActive.set(true);
  }

  onIngredientsConfirmed(ingredients: RecognizedIngredient[]): void {
    this.scannerActive.set(false);
    this.recognizedIngredients.set(ingredients);
  }

  onScannerCancelled(): void {
    this.scannerActive.set(false);
  }

  dismissRecognizedIngredients(): void {
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
        // beat before we navigate. Clicking "Import another" cancels the
        // navigation by calling onDiscardRecipe().
        this.saveSuccess.set(saved.title);
        this.navigateAfterSaveTimer = setTimeout(() => {
          this.navigateAfterSaveTimer = null;
          void this.router.navigate([ROUTES.recipes.detail(saved.identifier)]);
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

  onImportAnother(): void {
    this.onDiscardRecipe();
    this.importSectionExpanded.set(true);
  }

  private cancelNavigateAfterSave(): void {
    if (this.navigateAfterSaveTimer !== null) {
      clearTimeout(this.navigateAfterSaveTimer);
      this.navigateAfterSaveTimer = null;
    }
  }

  private extractUrl(text: string | undefined): string | null {
    if (!text) return null;
    const [url] = text.match(/https?:\/\/\S+/i) ?? [];
    return url ?? null;
  }

  private resetImportState(): void {
    this.serverError.set(null);
    this.extractedRecipe.set(null);
    this.saveSuccess.set(null);
    this.isManualEntry.set(false);
  }
}
