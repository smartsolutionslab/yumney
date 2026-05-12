import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  effect,
  HostListener,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ChatApiService, RecipeApiService, type RecipeDetail } from '../api';
import {
  CookingTimerService,
  createAsyncState,
  ERROR_MAPS,
  ROUTES,
  UserPreferencesService,
  type VoiceCommand,
  VoiceService,
  WakeLockService,
} from '@yumney/shared/models';
import { LucideAngularModule } from 'lucide-angular';
import {
  CookingTimerComponent,
  LoadingSpinnerComponent,
  MessageBannerComponent,
  SideSheetComponent,
  StepDisplayComponent,
  VoiceIndicatorComponent,
} from '@yumney/ui';

const SWIPE_THRESHOLD = 50;

@Component({
  selector: 'yn-cook-mode',
  imports: [
    TranslocoModule,
    LucideAngularModule,
    StepDisplayComponent,
    CookingTimerComponent,
    VoiceIndicatorComponent,
    LoadingSpinnerComponent,
    MessageBannerComponent,
    SideSheetComponent,
  ],
  templateUrl: './cook-mode.component.html',
  styleUrl: './cook-mode.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CookModeComponent implements OnInit, OnDestroy {
  protected readonly ROUTES = ROUTES;

  private recipeApi = inject(RecipeApiService);
  private chatApi = inject(ChatApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private loadState = createAsyncState(this.destroyRef);
  protected voice = inject(VoiceService);
  protected timers = inject(CookingTimerService);
  private wakeLock = inject(WakeLockService);
  private transloco = inject(TranslocoService);
  private preferences = inject(UserPreferencesService);

  recipe = signal<RecipeDetail | null>(null);
  currentStepIndex = signal(0);
  ingredientsPanelOpen = signal(false);
  isLoading = this.loadState.isLoading;
  serverError = signal<string | null>(null);

  totalSteps = computed(() => this.recipe()?.steps.length ?? 0);
  currentStep = computed(() => {
    const recipe = this.recipe();
    return recipe?.steps[this.currentStepIndex()] ?? null;
  });

  private touchStartX: number | null = null;

  constructor() {
    effect(() => {
      const step = this.currentStep();
      if (step && this.preferences.voiceAutoReadInCookMode()) {
        this.voice.speak(step.description);
      }
    });
  }

  ngOnInit(): void {
    const identifier = this.route.snapshot.paramMap.get('identifier');
    if (!identifier) {
      this.serverError.set('recipes.cook.errors.notFound');
      return;
    }

    this.preferences.ensureLoaded();
    this.voice.setLanguage(this.transloco.getActiveLang() as 'en' | 'de');

    this.loadState.execute(
      this.recipeApi.getRecipeById(identifier),
      ERROR_MAPS.recipes.detail,
      (recipe) => {
        this.recipe.set(recipe);
        // Auto-activate continuous listening on entry per US-362 AC. Stays
        // silent if Web Speech isn't supported — the existing toggle button
        // remains available either way. Cleanup happens in `ngOnDestroy`.
        this.startListeningInCookMode();
      },
      (error) => this.serverError.set(error),
    );

    void this.wakeLock.acquire();
  }

  ngOnDestroy(): void {
    this.voice.stopSpeaking();
    this.voice.stopListening();
    this.timers.cancelAll();
    void this.wakeLock.release();
  }

  next(): void {
    if (this.currentStepIndex() < this.totalSteps() - 1) {
      this.currentStepIndex.update((index) => index + 1);
    }
  }

  previous(): void {
    if (this.currentStepIndex() > 0) {
      this.currentStepIndex.update((index) => index - 1);
    }
  }

  repeat(): void {
    const step = this.currentStep();
    if (step) {
      this.voice.speak(step.description);
    }
  }

  toggleIngredientsPanel(): void {
    this.ingredientsPanelOpen.update((open) => !open);
  }

  toggleListening(): void {
    if (this.voice.isListening()) {
      this.voice.stopListening();
    } else {
      this.startListeningInCookMode();
    }
  }

  private startListeningInCookMode(): void {
    this.voice.startListeningWithFallback(
      (command) => this.handleCommand(command),
      (transcript) => this.handleTranscript(transcript),
    );
  }

  private handleTranscript(transcript: string): void {
    // Global voice command while cooking — route through chat so users can say
    // "add butter to shopping list" or "what's for dinner tomorrow?" without
    // leaving cook mode. The reply gets spoken via the TTS path used by the
    // step-read effect.
    this.chatApi
      .send({ message: transcript, history: [] })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => this.voice.speak(response.reply),
        error: () => undefined,
      });
  }

  toggleMute(): void {
    this.voice.setMuted(!this.voice.muted());
  }

  exit(): void {
    const recipe = this.recipe();
    const id = recipe?.identifier;

    // Treat reaching the last step as "I cooked this" (US-121). Fire-and-forget
    // — failing to log shouldn't block the navigation. The server collapses
    // multiple opens within a window via dedup; cook tracking is intentionally
    // not deduped because each cook is a discrete completion.
    if (recipe && this.totalSteps() > 0 && this.currentStepIndex() >= this.totalSteps() - 1) {
      this.recipeApi.trackCooked(recipe.identifier).subscribe({
        error: () => undefined,
      });
    }

    if (id) {
      void this.router.navigate([ROUTES.recipes.detail(id)]);
    } else {
      void this.router.navigate([ROUTES.recipes.list]);
    }
  }

  onCancelTimer(id: string): void {
    this.timers.cancel(id);
  }

  @HostListener('touchstart', ['$event'])
  onTouchStart(event: TouchEvent): void {
    this.touchStartX = event.touches[0]?.clientX ?? null;
  }

  @HostListener('touchend', ['$event'])
  onTouchEnd(event: TouchEvent): void {
    if (this.touchStartX === null) return;
    const endX = event.changedTouches[0]?.clientX ?? this.touchStartX;
    const delta = endX - this.touchStartX;
    if (Math.abs(delta) >= SWIPE_THRESHOLD) {
      if (delta < 0) this.next();
      else this.previous();
    }
    this.touchStartX = null;
  }

  @HostListener('document:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'ArrowRight') this.next();
    else if (event.key === 'ArrowLeft') this.previous();
    else if (event.key === 'Escape') this.exit();
  }

  private handleCommand(command: VoiceCommand): void {
    switch (command.type) {
      case 'next':
        this.next();
        break;
      case 'previous':
        this.previous();
        break;
      case 'repeat':
        this.repeat();
        break;
      case 'stop':
        this.voice.stopSpeaking();
        this.timers.cancelAll();
        break;
      case 'ingredients':
        this.ingredientsPanelOpen.set(true);
        break;
      case 'timer': {
        const name = this.transloco.translate('recipes.cook.timer.defaultName');
        this.timers.start(name, command.minutes);
        break;
      }
    }
  }
}
