import { Injectable, inject, signal } from '@angular/core';
import { UserProfileApiService } from '@yumney/shared/api-client';

/**
 * Lightweight in-memory cache of the parts of the user profile that
 * non-account features (cooking-timer, voice-controls, etc.) need to
 * read. Keeps the wider profile fetch confined to the account MFE while
 * still letting other features react to NotificationPreferences without
 * each pulling the full profile on every interaction.
 *
 * Defaults match the historical hard-coded behaviour (vibrate + speak)
 * so that anything firing before the first `ensureLoaded()` resolves
 * still alerts the user.
 */
@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private readonly api = inject(UserProfileApiService);

  readonly timerHapticFeedback = signal<boolean>(true);
  readonly timerSoundAlerts = signal<boolean>(true);

  private loaded = false;

  ensureLoaded(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.refresh();
  }

  refresh(): void {
    this.api.getProfile().subscribe({
      next: (profile) => {
        this.timerHapticFeedback.set(profile.notificationPreferences.timerHapticFeedback);
        this.timerSoundAlerts.set(profile.notificationPreferences.timerSoundAlerts);
      },
      error: () => {
        // Keep current/default values on transient failure.
      },
    });
  }
}
