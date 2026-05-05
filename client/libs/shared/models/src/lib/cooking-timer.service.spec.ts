import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CookingTimerService } from './cooking-timer.service';
import { UserPreferencesService } from './user-preferences.service';
import { VoiceService } from './voice.service';

interface PrefsStub {
  timerHapticFeedback: ReturnType<typeof signal<boolean>>;
  timerSoundAlerts: ReturnType<typeof signal<boolean>>;
  ensureLoaded: () => void;
  refresh: () => void;
}

function makePrefsStub(haptic = true, sound = true): PrefsStub {
  return {
    timerHapticFeedback: signal(haptic),
    timerSoundAlerts: signal(sound),
    ensureLoaded: vi.fn(),
    refresh: vi.fn(),
  };
}

function configure(prefs: PrefsStub): {
  service: CookingTimerService;
  voiceSpeak: ReturnType<typeof vi.fn>;
  vibrate: ReturnType<typeof vi.fn>;
} {
  const voiceSpeak = vi.fn();
  const vibrate = vi.fn();
  Object.defineProperty(navigator, 'vibrate', { configurable: true, value: vibrate });

  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      CookingTimerService,
      { provide: VoiceService, useValue: { speak: voiceSpeak } },
      { provide: UserPreferencesService, useValue: prefs },
    ],
  });

  return { service: TestBed.inject(CookingTimerService), voiceSpeak, vibrate };
}

describe('CookingTimerService', () => {
  let service: CookingTimerService;
  let voiceSpeak: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.useFakeTimers();
    const wired = configure(makePrefsStub());
    service = wired.service;
    voiceSpeak = wired.voiceSpeak;
  });

  afterEach(() => {
    service.cancelAll();
    vi.useRealTimers();
  });

  it('should start a timer with correct totalSeconds', () => {
    const id = service.start('Pasta', 1);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.totalSeconds).toBe(60);
  });

  it('should set initial remainingSeconds equal to totalSeconds', () => {
    const id = service.start('Pasta', 2);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.remainingSeconds).toBe(120);
  });

  it('should mark hasActive true while a timer is running', () => {
    service.start('Pasta', 1);

    expect(service.hasActive()).toBe(true);
  });

  it('should decrement remainingSeconds each second', () => {
    const id = service.start('Pasta', 1);

    vi.advanceTimersByTime(3000);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.remainingSeconds).toBe(57);
  });

  it('should mark timer completed when countdown reaches zero', () => {
    const id = service.start('Quick', 1 / 60); // 1 second

    vi.advanceTimersByTime(1100);
    const timer = service.all().find((t) => t.id === id);

    expect(timer?.status).toBe('completed');
  });

  it('should announce completion via voice service', () => {
    service.start('Quick', 1 / 60);

    vi.advanceTimersByTime(1100);

    expect(voiceSpeak).toHaveBeenCalledWith('Timer done');
  });

  it('should remove timer when canceled', () => {
    const id = service.start('Pasta', 5);

    service.cancel(id);

    expect(service.all().find((t) => t.id === id)).toBeUndefined();
  });

  it('should cancel all timers via cancelAll', () => {
    service.start('A', 1);
    service.start('B', 1);

    service.cancelAll();

    expect(service.all()).toHaveLength(0);
  });

  describe('NotificationPreferences gating', () => {
    afterEach(() => {
      service.cancelAll();
    });

    it('should fire both vibrate and speak when both prefs are on', () => {
      const wired = configure(makePrefsStub(true, true));
      service = wired.service;

      wired.service.start('Quick', 1 / 60);
      vi.advanceTimersByTime(1100);

      expect(wired.vibrate).toHaveBeenCalled();
      expect(wired.voiceSpeak).toHaveBeenCalledWith('Timer done');
    });

    it('should stay silent when both prefs are off', () => {
      const wired = configure(makePrefsStub(false, false));
      service = wired.service;

      wired.service.start('Quick', 1 / 60);
      vi.advanceTimersByTime(1100);

      expect(wired.vibrate).not.toHaveBeenCalled();
      expect(wired.voiceSpeak).not.toHaveBeenCalled();
    });

    it('should only vibrate when haptic is on and sound is off', () => {
      const wired = configure(makePrefsStub(true, false));
      service = wired.service;

      wired.service.start('Quick', 1 / 60);
      vi.advanceTimersByTime(1100);

      expect(wired.vibrate).toHaveBeenCalled();
      expect(wired.voiceSpeak).not.toHaveBeenCalled();
    });

    it('should only speak when sound is on and haptic is off', () => {
      const wired = configure(makePrefsStub(false, true));
      service = wired.service;

      wired.service.start('Quick', 1 / 60);
      vi.advanceTimersByTime(1100);

      expect(wired.vibrate).not.toHaveBeenCalled();
      expect(wired.voiceSpeak).toHaveBeenCalledWith('Timer done');
    });
  });
});
