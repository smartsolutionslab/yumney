import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { UserPreferencesService } from './user-preferences.service';
import { VoiceService } from './voice.service';

describe('VoiceService.parseCommand', () => {
  it('should parse "next step" as next command', () => {
    expect(VoiceService.parseCommand('next step')).toEqual({ type: 'next' });
  });

  it('should parse German "nächster schritt" as next command', () => {
    expect(VoiceService.parseCommand('nächster schritt')).toEqual({ type: 'next' });
  });

  it('should parse "previous step" as previous command', () => {
    expect(VoiceService.parseCommand('previous step')).toEqual({ type: 'previous' });
  });

  it('should parse German "vorheriger schritt" as previous command', () => {
    expect(VoiceService.parseCommand('vorheriger schritt')).toEqual({ type: 'previous' });
  });

  it('should parse "repeat" as repeat command', () => {
    expect(VoiceService.parseCommand('repeat')).toEqual({ type: 'repeat' });
  });

  it('should parse "stop" as stop command', () => {
    expect(VoiceService.parseCommand('stop')).toEqual({ type: 'stop' });
  });

  it('should parse German "stopp" as stop command', () => {
    expect(VoiceService.parseCommand('stopp')).toEqual({ type: 'stop' });
  });

  it('should parse "ingredients" as ingredients command', () => {
    expect(VoiceService.parseCommand('ingredients')).toEqual({ type: 'ingredients' });
  });

  it('should parse German "zutaten" as ingredients command', () => {
    expect(VoiceService.parseCommand('zutaten')).toEqual({ type: 'ingredients' });
  });

  it('should parse "timer 5 minutes" as timer command with minutes', () => {
    expect(VoiceService.parseCommand('timer 5 minutes')).toEqual({ type: 'timer', minutes: 5 });
  });

  it('should parse German "timer 10 minuten" as timer command with minutes', () => {
    expect(VoiceService.parseCommand('timer 10 minuten')).toEqual({ type: 'timer', minutes: 10 });
  });

  it('should parse "5 minute timer" as timer command', () => {
    expect(VoiceService.parseCommand('5 minute timer')).toEqual({ type: 'timer', minutes: 5 });
  });

  it('should be case-insensitive', () => {
    expect(VoiceService.parseCommand('NEXT STEP')).toEqual({ type: 'next' });
  });

  it('should ignore surrounding whitespace', () => {
    expect(VoiceService.parseCommand('  next step  ')).toEqual({ type: 'next' });
  });

  it('should return null for unknown phrases', () => {
    expect(VoiceService.parseCommand('do something random')).toBeNull();
  });

  it('should return null for empty input', () => {
    expect(VoiceService.parseCommand('')).toBeNull();
  });
});

interface PrefsStub {
  voiceEnabled: ReturnType<typeof signal<boolean>>;
  voiceSpeed: ReturnType<typeof signal<'slow' | 'normal' | 'fast'>>;
  ensureLoaded: ReturnType<typeof vi.fn>;
}

function makePrefsStub(enabled: boolean, speed: 'slow' | 'normal' | 'fast' = 'normal'): PrefsStub {
  return {
    voiceEnabled: signal(enabled),
    voiceSpeed: signal(speed),
    ensureLoaded: vi.fn(),
  };
}

interface UtteranceShape {
  text: string;
  lang: string;
  rate: number;
  onstart: (() => void) | null;
  onend: (() => void) | null;
  onerror: (() => void) | null;
}

function configureSpeak(prefs: PrefsStub): {
  service: VoiceService;
  speak: ReturnType<typeof vi.fn>;
  cancel: ReturnType<typeof vi.fn>;
  lastUtterance: { value: UtteranceShape | null };
} {
  const speak = vi.fn();
  const cancel = vi.fn();
  const lastUtterance = { value: null as UtteranceShape | null };
  speak.mockImplementation((utterance: UtteranceShape) => {
    lastUtterance.value = utterance;
  });
  // jsdom doesn't ship SpeechSynthesis APIs; stub the bits VoiceService.speak touches.
  Object.defineProperty(window, 'speechSynthesis', {
    configurable: true,
    value: { speak, cancel },
  });
  (globalThis as unknown as { SpeechSynthesisUtterance: unknown }).SpeechSynthesisUtterance =
    class implements UtteranceShape {
      lang = '';
      rate = 1;
      onstart: (() => void) | null = null;
      onend: (() => void) | null = null;
      onerror: (() => void) | null = null;
      constructor(public text: string) {}
    };

  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [VoiceService, { provide: UserPreferencesService, useValue: prefs }],
  });

  return { service: TestBed.inject(VoiceService), speak, cancel, lastUtterance };
}

describe('VoiceService.speak', () => {
  it('triggers speechSynthesis.speak when voice is enabled', () => {
    const { service, speak } = configureSpeak(makePrefsStub(true));

    service.speak('hello');

    expect(speak).toHaveBeenCalledTimes(1);
  });

  it('skips speaking when the voiceEnabled preference is false', () => {
    const { service, speak } = configureSpeak(makePrefsStub(false));

    service.speak('hello');

    expect(speak).not.toHaveBeenCalled();
  });

  it('skips speaking when the session mute toggle is on, even if preference is enabled', () => {
    const { service, speak } = configureSpeak(makePrefsStub(true));
    service.setMuted(true);

    service.speak('hello');

    expect(speak).not.toHaveBeenCalled();
  });

  it('applies a slower rate when voiceSpeed preference is "slow"', () => {
    const { service, lastUtterance } = configureSpeak(makePrefsStub(true, 'slow'));

    service.speak('hello');

    expect(lastUtterance.value?.rate).toBeLessThan(1);
  });

  it('applies a faster rate when voiceSpeed preference is "fast"', () => {
    const { service, lastUtterance } = configureSpeak(makePrefsStub(true, 'fast'));

    service.speak('hello');

    expect(lastUtterance.value?.rate).toBeGreaterThan(1);
  });

  it('lazy-loads preferences the first time speak is called', () => {
    const prefs = makePrefsStub(true);
    const { service } = configureSpeak(prefs);

    service.speak('hello');

    expect(prefs.ensureLoaded).toHaveBeenCalledTimes(1);
  });
});

interface MockRecognition {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  start: ReturnType<typeof vi.fn>;
  stop: ReturnType<typeof vi.fn>;
  onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
  onerror: ((event: { error: string }) => void) | null;
  onend: (() => void) | null;
}

function installSpeechRecognition(): () => MockRecognition {
  let last: MockRecognition | null = null;
  const remember = (instance: MockRecognition): void => {
    last = instance;
  };

  function MockCtor(this: MockRecognition): void {
    this.lang = '';
    this.continuous = false;
    this.interimResults = false;
    this.start = vi.fn();
    this.stop = vi.fn();
    this.onresult = null;
    this.onerror = null;
    this.onend = null;
    remember(this);
  }

  (window as unknown as { SpeechRecognition?: unknown }).SpeechRecognition = MockCtor;
  return () => {
    if (!last) throw new Error('SpeechRecognition was not constructed');
    return last;
  };
}

describe('VoiceService.startListeningForTranscript (US-360)', () => {
  let service: VoiceService;
  let getRecognition: () => MockRecognition;

  beforeEach(() => {
    getRecognition = installSpeechRecognition();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [VoiceService, { provide: UserPreferencesService, useValue: makePrefsStub(true) }],
    });
    service = TestBed.inject(VoiceService);
  });

  afterEach(() => {
    delete (window as unknown as { SpeechRecognition?: unknown }).SpeechRecognition;
  });

  it('starts a single-utterance session', () => {
    service.startListeningForTranscript(() => undefined);

    const recognition = getRecognition();
    expect(recognition.continuous).toBe(false);
    expect(recognition.interimResults).toBe(false);
    expect(recognition.start).toHaveBeenCalled();
    expect(service.isListening()).toBe(true);
  });

  it('emits the raw transcript, trimmed', () => {
    const callback = vi.fn();
    service.startListeningForTranscript(callback);

    getRecognition().onresult?.({ results: [[{ transcript: ' add milk ' }]] });

    expect(callback).toHaveBeenCalledWith('add milk');
  });

  it('does not emit for empty transcripts', () => {
    const callback = vi.fn();
    service.startListeningForTranscript(callback);

    getRecognition().onresult?.({ results: [[{ transcript: '   ' }]] });

    expect(callback).not.toHaveBeenCalled();
  });

  it('uses the configured language', () => {
    service.setLanguage('de');
    service.startListeningForTranscript(() => undefined);

    expect(getRecognition().lang).toBe('de-DE');
  });

  it('clears isListening on end', () => {
    service.startListeningForTranscript(() => undefined);

    getRecognition().onend?.();

    expect(service.isListening()).toBe(false);
  });

  it('clears isListening on error', () => {
    service.startListeningForTranscript(() => undefined);

    getRecognition().onerror?.({ error: 'no-speech' });

    expect(service.isListening()).toBe(false);
  });

  it('does nothing when already listening', () => {
    service.startListeningForTranscript(() => undefined);
    const first = getRecognition();
    first.start.mockClear();

    service.startListeningForTranscript(() => undefined);

    expect(first.start).not.toHaveBeenCalled();
  });
});
