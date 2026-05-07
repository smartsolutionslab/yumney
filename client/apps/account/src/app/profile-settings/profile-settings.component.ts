import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { debounceTime, skip } from 'rxjs';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import {
  createAsyncState,
  ERROR_MAPS,
  ThemeService,
  UI,
  VoiceService,
} from '@yumney/shared/models';
import { AsyncStateComponent, SettingsCardComponent } from '@yumney/ui';
import { UserProfileApiService, type UpdateProfileRequest, type UserProfile } from '../api';

const AUTOSAVE_DEBOUNCE_MS = 400;

@Component({
  selector: 'yn-profile-settings',
  standalone: true,
  imports: [FormsModule, TranslocoModule, AsyncStateComponent, SettingsCardComponent],
  templateUrl: './profile-settings.component.html',
  styleUrl: './profile-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileSettingsComponent {
  private api = inject(UserProfileApiService);
  private destroyRef = inject(DestroyRef);
  private theme = inject(ThemeService);
  private voice = inject(VoiceService);
  private transloco = inject(TranslocoService);
  private loadState = createAsyncState(this.destroyRef);
  private saveState = createAsyncState(this.destroyRef);
  private autosaveTick = signal(0);
  private autosave$ = toObservable(this.autosaveTick);

  protected profile = signal<UserProfile | null>(null);
  protected loading = this.loadState.isLoading;
  protected saving = this.saveState.isLoading;
  protected error = computed(() => this.loadState.serverError() ?? this.saveState.serverError());
  protected saved = signal(false);

  // Identity
  protected displayName = signal('');
  protected email = signal('');

  // Language & units
  protected preferredLanguage = signal<'en' | 'de'>('en');
  protected preferredUnitSystem = signal<'metric' | 'imperial'>('metric');

  // Appearance
  protected themeChoice = signal<'light' | 'dark' | 'system'>('system');

  // Cooking
  protected defaultServings = signal(4);
  protected dietaryType = signal<string>('');
  protected restrictions = signal<string[]>([]);
  protected minVeggieMeals = signal<number | null>(null);
  protected maxRedMeatMeals = signal<number | null>(null);
  protected cookingEffort = signal<string>('');

  // Voice
  protected voiceEnabled = signal(true);
  protected voiceSpeed = signal<'slow' | 'normal' | 'fast'>('normal');
  protected voiceAutoRead = signal(false);

  // Notifications
  protected timerHaptic = signal(true);
  protected timerSound = signal(true);

  protected readonly languages: Array<'en' | 'de'> = ['en', 'de'];
  protected readonly unitSystems: Array<'metric' | 'imperial'> = ['metric', 'imperial'];
  protected readonly themes: Array<'light' | 'dark' | 'system'> = ['light', 'dark', 'system'];
  protected readonly voiceSpeeds: Array<'slow' | 'normal' | 'fast'> = ['slow', 'normal', 'fast'];
  protected readonly dietaryTypes = [
    'omnivore',
    'vegetarian',
    'vegan',
    'pescatarian',
    'flexitarian',
  ];
  protected readonly availableRestrictions = [
    'gluten-free',
    'lactose-free',
    'nut-allergy',
    'egg-free',
    'soy-free',
    'shellfish-allergy',
    'halal',
    'kosher',
  ];
  protected readonly cookingEfforts = ['quick-weekdays', 'balanced', 'elaborate-weekends'];

  constructor() {
    this.autosave$
      .pipe(skip(1), debounceTime(AUTOSAVE_DEBOUNCE_MS), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.persist());

    effect(() => {
      // Push theme + voice settings into local services so the change is
      // visible immediately, even before the server round-trip resolves.
      this.theme.setTheme(this.themeChoice());
      this.voice.setMuted(!this.voiceEnabled());
      this.voice.setLanguage(this.preferredLanguage());
      const lang = this.preferredLanguage();
      if (this.transloco.getActiveLang() !== lang) {
        this.transloco.setActiveLang(lang);
      }
    });

    this.loadProfile();
  }

  protected onChange(): void {
    this.saved.set(false);
    this.autosaveTick.update((tick) => tick + 1);
  }

  protected onToggleRestriction(restriction: string): void {
    const current = this.restrictions();
    const next = current.includes(restriction)
      ? current.filter((entry) => entry !== restriction)
      : [...current, restriction];
    this.restrictions.set(next);
    this.onChange();
  }

  protected onRetry(): void {
    this.loadProfile();
  }

  private persist(): void {
    if (this.profile() === null) return;

    const request: UpdateProfileRequest = {
      displayName: this.displayName().trim() || null,
      preferredLanguage: this.preferredLanguage(),
      preferredUnitSystem: this.preferredUnitSystem(),
      defaultServings: this.defaultServings(),
      theme: this.themeChoice(),
      voiceSettings: {
        enabled: this.voiceEnabled(),
        speed: this.voiceSpeed(),
        autoReadInCookMode: this.voiceAutoRead(),
      },
      notificationPreferences: {
        timerHapticFeedback: this.timerHaptic(),
        timerSoundAlerts: this.timerSound(),
      },
      dietaryType: this.dietaryType() || null,
      restrictions: this.restrictions(),
      minVeggieMeals: this.minVeggieMeals(),
      maxRedMeatMeals: this.maxRedMeatMeals(),
      cookingEffort: this.cookingEffort() || null,
    };

    this.saveState.execute(this.api.updateProfile(request), ERROR_MAPS.account.save, (updated) => {
      this.profile.set(updated);
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), UI.SAVED_INDICATOR_MS);
    });
  }

  private loadProfile(): void {
    this.loadState.execute(this.api.getProfile(), ERROR_MAPS.account.load, (profile) => {
      this.profile.set(profile);
      this.displayName.set(profile.displayName);
      this.email.set(profile.email);
      this.preferredLanguage.set((profile.preferredLanguage === 'de' ? 'de' : 'en') as 'en' | 'de');
      this.preferredUnitSystem.set(
        (profile.preferredUnitSystem === 'imperial' ? 'imperial' : 'metric') as
          | 'metric'
          | 'imperial',
      );
      this.themeChoice.set(profile.theme);
      this.defaultServings.set(profile.defaultServings);
      this.dietaryType.set(profile.dietaryProfile.dietaryType ?? '');
      this.restrictions.set([...profile.dietaryProfile.restrictions]);
      this.minVeggieMeals.set(profile.dietaryProfile.minVeggieMeals);
      this.maxRedMeatMeals.set(profile.dietaryProfile.maxRedMeatMeals);
      this.cookingEffort.set(profile.dietaryProfile.cookingEffort ?? '');
      this.voiceEnabled.set(profile.voiceSettings.enabled);
      this.voiceSpeed.set(profile.voiceSettings.speed);
      this.voiceAutoRead.set(profile.voiceSettings.autoReadInCookMode);
      this.timerHaptic.set(profile.notificationPreferences.timerHapticFeedback);
      this.timerSound.set(profile.notificationPreferences.timerSoundAlerts);
    });
  }
}
