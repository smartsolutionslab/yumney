import { TestBed } from '@angular/core/testing';
import { UserProfileApiService } from '@yumney/shared/api-client';
import { Observable, of, throwError } from 'rxjs';
import { UserPreferencesService } from './user-preferences.service';

interface ProfileShape {
  notificationPreferences: {
    timerHapticFeedback: boolean;
    timerSoundAlerts: boolean;
  };
}

function configure(profileSource: Observable<ProfileShape>): {
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
  it('defaults to both flags on so first-call alerts still fire', () => {
    const { service } = configure(
      of({ notificationPreferences: { timerHapticFeedback: true, timerSoundAlerts: true } }),
    );

    expect(service.timerHapticFeedback()).toBe(true);
    expect(service.timerSoundAlerts()).toBe(true);
  });

  it('ensureLoaded fetches the profile and writes both flags', () => {
    const { service, api } = configure(
      of({ notificationPreferences: { timerHapticFeedback: false, timerSoundAlerts: false } }),
    );

    service.ensureLoaded();

    expect(api.getProfile).toHaveBeenCalledTimes(1);
    expect(service.timerHapticFeedback()).toBe(false);
    expect(service.timerSoundAlerts()).toBe(false);
  });

  it('ensureLoaded only fetches once across repeated calls', () => {
    const { service, api } = configure(
      of({ notificationPreferences: { timerHapticFeedback: true, timerSoundAlerts: true } }),
    );

    service.ensureLoaded();
    service.ensureLoaded();
    service.ensureLoaded();

    expect(api.getProfile).toHaveBeenCalledTimes(1);
  });

  it('refresh fetches even after ensureLoaded has run', () => {
    const { service, api } = configure(
      of({ notificationPreferences: { timerHapticFeedback: true, timerSoundAlerts: true } }),
    );

    service.ensureLoaded();
    service.refresh();

    expect(api.getProfile).toHaveBeenCalledTimes(2);
  });

  it('keeps current signal values when the profile fetch errors', () => {
    const { service } = configure(throwError(() => new Error('boom')));

    service.ensureLoaded();

    expect(service.timerHapticFeedback()).toBe(true);
    expect(service.timerSoundAlerts()).toBe(true);
  });
});
