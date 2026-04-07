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
  DashboardApiService,
  type UserActivityItem,
  type SuggestionsResponse,
} from '@yumney/shared/api-client';
import {
  urlValidator,
  createAsyncState,
  mapToSaveRecipeRequest,
  VALIDATION,
  ERROR_MAPS,
  ROUTES,
  ensureFormValid,
} from '@yumney/shared/models';
import {
  RecipePreviewComponent,
  FormFieldComponent,
  SubmitButtonComponent,
  QuickActionsComponent,
  type QuickAction,
  SuggestionCardComponent,
  RecentActivityComponent,
  CameraCaptureComponent,
  IngredientScannerComponent,
  ShareToastComponent,
  IngredientsToastComponent,
} from '@yumney/ui';
import { CameraService } from '@yumney/shared/models';
import type { RecognizedIngredient } from '@yumney/shared/api-client';

@Component({
  selector: 'yn-dashboard',
  imports: [
    ReactiveFormsModule,
    TranslocoModule,
    RecipePreviewComponent,
    FormFieldComponent,
    SubmitButtonComponent,
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
})
export class DashboardComponent implements OnInit {
  private formBuilder = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private recipeApi = inject(RecipeApiService);
  private dashboardApi = inject(DashboardApiService);
  protected camera = inject(CameraService);
  private destroyRef = inject(DestroyRef);
  private importState = createAsyncState(this.destroyRef);
  private saveState = createAsyncState(this.destroyRef);

  isLoading = this.importState.isLoading;
  isSaving = this.saveState.isLoading;
  serverError = signal<string | null>(null);
  extractedRecipe = signal<ImportRecipeResponse | null>(null);
  sourceUrl = signal<string | null>(null);
  saveSuccess = signal<string | null>(null);
  isManualEntry = signal(false);
  streamingStatus = signal<string | null>(null);
  streamingChunks = signal('');
  importSectionExpanded = signal(false);
  cameraActive = signal(false);
  scannerActive = signal(false);
  shareToast = signal<string | null>(null);
  recognizedIngredients = signal<RecognizedIngredient[] | null>(null);

  // Smart dashboard state
  quickActions = signal<QuickAction[]>([]);
  suggestions = signal<SuggestionsResponse | null>(null);
  recentActivity = signal<UserActivityItem[]>([]);
  suggestionsLoading = signal(true);

  form = this.formBuilder.nonNullable.group({
    url: [
      '',
      [
        Validators.required,
        Validators.maxLength(VALIDATION.RECIPES.RECIPE_URL.MAX_LENGTH),
        urlValidator,
      ],
    ],
  });

  ngOnInit(): void {
    this.loadDashboardData();

    // Subscribe to query params so subsequent shares (when app already open) also work
    this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const urlParam = params['url'] as string | undefined;
      const textParam = params['text'] as string | undefined;
      const sharedText = urlParam || textParam;

      if (!sharedText) return;

      const sharedUrl = urlParam || this.extractUrl(textParam);

      if (sharedUrl) {
        this.handleSharedUrl(sharedUrl);
      } else if (textParam) {
        // Shared text without a recognizable URL
        this.serverError.set('dashboard.share.noUrlFound');
        this.importSectionExpanded.set(true);
      }
    });
  }

  private handleSharedUrl(sharedUrl: string): void {
    this.form.controls.url.setValue(sharedUrl);
    this.importSectionExpanded.set(true);
    this.shareToast.set(sharedUrl);
    this.onImport();
  }

  dismissShareToast(): void {
    this.shareToast.set(null);
  }

  toggleImportSection(): void {
    this.importSectionExpanded.update((v) => !v);
  }

  onQuickAction(actionKey: string): void {
    if (actionKey === 'cook_now') {
      this.router.navigate([ROUTES.recipes.list]);
    } else {
      this.importSectionExpanded.set(true);
    }
  }

  private loadDashboardData(): void {
    this.dashboardApi
      .getSuggestions()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((response) => {
        this.suggestions.set(response);
        this.quickActions.set(this.mapQuickActions(response.quickActions));
        this.suggestionsLoading.set(false);

        if (response.quickActions.length === 0 && response.suggestions.length === 0) {
          this.importSectionExpanded.set(true);
        }
      });

    this.dashboardApi
      .getRecentActivity(5)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((activity) => this.recentActivity.set(activity));
  }

  private mapQuickActions(keys: string[]): QuickAction[] {
    return keys.map((key) => ({
      key,
      labelKey: 'dashboard.quickActions.' + key,
    }));
  }

  onImport(): void {
    if (!ensureFormValid(this.form)) return;

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

    this.saveState.execute(
      this.recipeApi.saveRecipe(request),
      ERROR_MAPS.dashboard.save,
      (saved) => {
        this.router.navigate([ROUTES.recipes.detail(saved.identifier)]);
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

    const [url] = text.match(/https?:\/\/\S+/i) ?? [];
    return url ?? null;
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
