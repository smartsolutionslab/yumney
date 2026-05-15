import { Injectable, inject, signal } from '@angular/core';
import { UserProfileApiService, type UserProfile } from '@yumney/shared/api-client';
import type { UnitSystem } from './unit-conversion';

/**
 * Lightweight in-memory cache of the parts of the user profile that
 * non-account features (cooking-timer, voice-controls, etc.) need to
 * read. Keeps the wider profile fetch confined to the account MFE while
 * still letting other features react to NotificationPreferences without
 * each pulling the full profile on every interaction.
 *
 * Defaults match the historical hard-coded behaviour (vibrate + speak,
 * voice on at normal speed, no auto-read) so that anything firing before
 * the first `ensureLoaded()` resolves still behaves sensibly.
 */
@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private readonly api = inject(UserProfileApiService);

  readonly timerHapticFeedback = signal<boolean>(true);
  readonly timerSoundAlerts = signal<boolean>(true);
  readonly voiceEnabled = signal<boolean>(true);
  readonly voiceSpeed = signal<'slow' | 'normal' | 'fast'>('normal');
  readonly voiceAutoReadInCookMode = signal<boolean>(false);

  // The user's `preferredUnitSystem` from their profile (US-100). Seeded by
  // applyProfile / refresh. Consumers (recipe detail's unit-system toggle)
  // use this as the initial value so a returning imperial-by-default user
  // sees imperial right away — the metric-only fallback was the open AC in
  // US-125. Defaults to metric until the first profile load resolves.
  readonly preferredUnitSystem = signal<UnitSystem>('metric');

  private loaded = false;

  ensureLoaded(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.refresh();
  }

  refresh(): void {
    this.api.getProfile().subscribe({
      next: (profile) => this.applyProfile(profile),
      error: () => {
        // Keep current/default values on transient failure.
      },
    });
  }

  /**
   * Fast-path used by the account settings page after a successful save:
   * pushes the freshly-persisted profile straight into the cache so other
   * features see the new values without a follow-up GET.
   */
  applyProfile(profile: UserProfile): void {
    this.loaded = true;
    this.timerHapticFeedback.set(profile.notificationPreferences.timerHapticFeedback);
    this.timerSoundAlerts.set(profile.notificationPreferences.timerSoundAlerts);
    this.voiceEnabled.set(profile.voiceSettings.enabled);
    this.voiceSpeed.set(profile.voiceSettings.speed);
    this.voiceAutoReadInCookMode.set(profile.voiceSettings.autoReadInCookMode);
    this.preferredUnitSystem.set(profile.preferredUnitSystem === 'imperial' ? 'imperial' : 'metric');
  }
}
