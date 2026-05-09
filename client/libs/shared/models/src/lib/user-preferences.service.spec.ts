import { TestBed } from '@angular/core/testing';
import { UserProfileApiService, type UserProfile } from '@yumney/shared/api-client';
import { Observable, of, throwError } from 'rxjs';
import { UserPreferencesService } from './user-preferences.service';

function makeProfile(overrides: Partial<UserProfile> = {}): UserProfile {
  return {
    displayName: 'Test User',
    email: 'test@example.com',
    preferredLanguage: 'en',
    preferredUnitSystem: 'metric',
    defaultServings: 4,
    theme: 'system',
    voiceSettings: { enabled: true, speed: 'normal', autoReadInCookMode: false },
    notificationPreferences: { timerHapticFeedback: true, timerSoundAlerts: true },
    dietaryProfile: {
      dietaryType: null,
      restrictions: [],
      minVeggieMeals: null,
      maxRedMeatMeals: null,
      cookingEffort: null,
    },
    ...overrides,
  };
}

function configure(profileSource: Observable<UserProfile>): {
  service: UserPreferencesService;
  api: { getProfile: ReturnType<typeof vi.fn> };
} {
  const api = { getProfile: vi.fn().mockReturnValue(profileSource) };
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [{ provide: UserProfileApiService, useValue: api }],
  });
  return { service: TestBed.inject(UserPreferencesService), api };
}

describe('UserPreferencesService', () => {
  it('defaults to sensible values so first-call behaviour is preserved', () => {
    const { service } = configure(of(makeProfile()));

    expect(service.timerHapticFeedback()).toBe(true);
    expect(service.timerSoundAlerts()).toBe(true);
    expect(service.voiceEnabled()).toBe(true);
    expect(service.voiceSpeed()).toBe('normal');
    expect(service.voiceAutoReadInCookMode()).toBe(false);
  });

  it('ensureLoaded fetches the profile and applies notification + voice settings', () => {
    const { service, api } = configure(
      of(
        makeProfile({
          notificationPreferences: { timerHapticFeedback: false, timerSoundAlerts: false },
          voiceSettings: { enabled: false, speed: 'fast', autoReadInCookMode: true },
        }),
      ),
    );

    service.ensureLoaded();

    expect(api.getProfile).toHaveBeenCalledTimes(1);
    expect(service.timerHapticFeedback()).toBe(false);
    expect(service.timerSoundAlerts()).toBe(false);
    expect(service.voiceEnabled()).toBe(false);
    expect(service.voiceSpeed()).toBe('fast');
    expect(service.voiceAutoReadInCookMode()).toBe(true);
  });

  it('ensureLoaded only fetches once across repeated calls', () => {
    const { service, api } = configure(of(makeProfile()));

    service.ensureLoaded();
    service.ensureLoaded();
    service.ensureLoaded();

    expect(api.getProfile).toHaveBeenCalledTimes(1);
  });

  it('refresh fetches even after ensureLoaded has run', () => {
    const { service, api } = configure(of(makeProfile()));

    service.ensureLoaded();
    service.refresh();

    expect(api.getProfile).toHaveBeenCalledTimes(2);
  });

  it('keeps current signal values when the profile fetch errors', () => {
    const { service } = configure(throwError(() => new Error('boom')));

    service.ensureLoaded();

    expect(service.timerHapticFeedback()).toBe(true);
    expect(service.timerSoundAlerts()).toBe(true);
    expect(service.voiceEnabled()).toBe(true);
    expect(service.voiceSpeed()).toBe('normal');
  });

  it('applyProfile pushes saved profile values without an extra GET', () => {
    const { service, api } = configure(of(makeProfile()));

    service.applyProfile(
      makeProfile({
        voiceSettings: { enabled: false, speed: 'slow', autoReadInCookMode: true },
        notificationPreferences: { timerHapticFeedback: false, timerSoundAlerts: false },
      }),
    );

    expect(api.getProfile).not.toHaveBeenCalled();
    expect(service.voiceEnabled()).toBe(false);
    expect(service.voiceSpeed()).toBe('slow');
    expect(service.voiceAutoReadInCookMode()).toBe(true);
    expect(service.timerHapticFeedback()).toBe(false);
    expect(service.timerSoundAlerts()).toBe(false);
  });

  it('applyProfile marks the cache as loaded so ensureLoaded becomes a no-op', () => {
    const { service, api } = configure(of(makeProfile()));

    service.applyProfile(makeProfile());
    service.ensureLoaded();

    expect(api.getProfile).not.toHaveBeenCalled();
  });
});
