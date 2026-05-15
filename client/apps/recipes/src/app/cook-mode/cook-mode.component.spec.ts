import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { signal } from '@angular/core';
import { CookModeComponent } from './cook-mode.component';
import { ChatApiService, RecipeApiService, type RecipeDetail } from '../api';
import { UserPreferencesService, VoiceService, WakeLockService, CookingTimerService, setupTranslocoTesting } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';

const mockRecipe: RecipeDetail = {
  identifier: 'test-id',
  title: 'Test Recipe',
  description: 'A test recipe',
  servings: 4,
  prepTimeMinutes: 10,
  cookTimeMinutes: 20,
  difficulty: 'easy',
  imageUrl: null,
  sourceUrl: null,
  createdAt: '2026-04-01T00:00:00Z',
  tags: [],
  isFavorite: false,
  ingredients: [
    { name: 'Flour', amount: 200, unit: 'g' },
    { name: 'Sugar', amount: 100, unit: 'g' },
  ],
  steps: [
    { number: 1, description: 'First step' },
    { number: 2, description: 'Second step' },
    { number: 3, description: 'Third step' },
  ],
};

const en = {
  recipes: {
    cook: {
      loading: 'Loading...',
      exit: 'Exit',
      mute: 'Mute',
      unmute: 'Unmute',
      ingredients: 'Ingredients',
      previous: 'Previous',
      repeat: 'Repeat',
      next: 'Next',
      voiceToggle: 'Voice toggle',
      voiceListening: 'Listening...',
      voiceTapToTalk: 'Tap to talk',
      timer: { defaultName: 'Timer' },
      errors: { notFound: 'Recipe not found.' },
    },
  },
};

describe('CookModeComponent', () => {
  let component: CookModeComponent;
  let fixture: ComponentFixture<CookModeComponent>;
  let recipeApiMock: { getRecipeById: ReturnType<typeof vi.fn> };
  let voiceMock: {
    speak: ReturnType<typeof vi.fn>;
    stopSpeaking: ReturnType<typeof vi.fn>;
    startListening: ReturnType<typeof vi.fn>;
    startListeningWithFallback: ReturnType<typeof vi.fn>;
    stopListening: ReturnType<typeof vi.fn>;
    setLanguage: ReturnType<typeof vi.fn>;
    setMuted: ReturnType<typeof vi.fn>;
    isListening: ReturnType<typeof signal<boolean>>;
    isSpeaking: ReturnType<typeof signal<boolean>>;
    muted: ReturnType<typeof signal<boolean>>;
    ttsSupported: ReturnType<typeof signal<boolean>>;
    sttSupported: ReturnType<typeof signal<boolean>>;
  };
  let chatApiMock: { send: ReturnType<typeof vi.fn> };
  let wakeLockMock: {
    acquire: ReturnType<typeof vi.fn>;
    release: ReturnType<typeof vi.fn>;
    supported: ReturnType<typeof signal<boolean>>;
    active: ReturnType<typeof signal<boolean>>;
  };
  let timersMock: {
    start: ReturnType<typeof vi.fn>;
    cancel: ReturnType<typeof vi.fn>;
    cancelAll: ReturnType<typeof vi.fn>;
    all: ReturnType<typeof signal<unknown[]>>;
    hasActive: ReturnType<typeof signal<boolean>>;
  };
  let preferencesMock: {
    voiceEnabled: ReturnType<typeof signal<boolean>>;
    voiceSpeed: ReturnType<typeof signal<'slow' | 'normal' | 'fast'>>;
    voiceAutoReadInCookMode: ReturnType<typeof signal<boolean>>;
    timerHapticFeedback: ReturnType<typeof signal<boolean>>;
    timerSoundAlerts: ReturnType<typeof signal<boolean>>;
    ensureLoaded: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
    applyProfile: ReturnType<typeof vi.fn>;
  };

  function setupTestBed(getRecipeByIdReturn: ReturnType<typeof vi.fn> = vi.fn(), identifier = 'test-id') {
    recipeApiMock = { getRecipeById: getRecipeByIdReturn };

    voiceMock = {
      speak: vi.fn(),
      stopSpeaking: vi.fn(),
      startListening: vi.fn(),
      startListeningWithFallback: vi.fn(),
      stopListening: vi.fn(),
      setLanguage: vi.fn(),
      setMuted: vi.fn(),
      isListening: signal(false),
      isSpeaking: signal(false),
      muted: signal(false),
      ttsSupported: signal(true),
      sttSupported: signal(true),
    };

    chatApiMock = {
      send: vi.fn().mockReturnValue(of({ reply: 'OK', suggestions: [] })),
    };

    wakeLockMock = {
      acquire: vi.fn().mockResolvedValue(undefined),
      release: vi.fn().mockResolvedValue(undefined),
      supported: signal(true),
      active: signal(false),
    };

    timersMock = {
      start: vi.fn(),
      cancel: vi.fn(),
      cancelAll: vi.fn(),
      all: signal([]),
      hasActive: signal(false),
    };

    preferencesMock = {
      voiceEnabled: signal(true),
      voiceSpeed: signal('normal'),
      voiceAutoReadInCookMode: signal(false),
      timerHapticFeedback: signal(true),
      timerSoundAlerts: signal(true),
      ensureLoaded: vi.fn(),
      refresh: vi.fn(),
      applyProfile: vi.fn(),
    };

    TestBed.configureTestingModule({
      imports: [CookModeComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: ChatApiService, useValue: chatApiMock },
        { provide: VoiceService, useValue: voiceMock },
        { provide: WakeLockService, useValue: wakeLockMock },
        { provide: CookingTimerService, useValue: timersMock },
        { provide: UserPreferencesService, useValue: preferencesMock },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'identifier' ? identifier : null),
              },
            },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(CookModeComponent);
    component = fixture.componentInstance;
  }

  it('should create the component', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component).toBeTruthy();
  }));

  it('should load recipe on init', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(recipeApiMock.getRecipeById).toHaveBeenCalledWith('test-id');
  }));

  it('should display first step after loading', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component.currentStepIndex()).toBe(0);
  }));

  it('should advance to next step on next()', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.next();

    expect(component.currentStepIndex()).toBe(1);
  }));

  it('should not advance past last step', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.next();
    component.next();
    component.next();

    expect(component.currentStepIndex()).toBe(2);
  }));

  it('should go back on previous()', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.next();
    component.previous();

    expect(component.currentStepIndex()).toBe(0);
  }));

  it('should not go back before first step', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.previous();

    expect(component.currentStepIndex()).toBe(0);
  }));

  it('should acquire wake lock on init', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(wakeLockMock.acquire).toHaveBeenCalled();
  }));

  it('should release wake lock on destroy', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    fixture.destroy();

    expect(wakeLockMock.release).toHaveBeenCalled();
  }));

  it('auto-starts continuous listening with cook+global fallback on entry (US-362)', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(voiceMock.startListeningWithFallback).toHaveBeenCalled();
    // Old single-path startListening should no longer be used in cook mode.
    expect(voiceMock.startListening).not.toHaveBeenCalled();
  }));

  it('re-starts listening with fallback when toggleListening is tapped off-then-on (US-362)', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();
    voiceMock.startListeningWithFallback.mockClear();

    voiceMock.isListening.set(true);
    component.toggleListening();
    expect(voiceMock.stopListening).toHaveBeenCalled();

    voiceMock.isListening.set(false);
    component.toggleListening();
    expect(voiceMock.startListeningWithFallback).toHaveBeenCalled();
  }));

  it('routes unparsed transcripts through chat and speaks the reply (US-362)', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    chatApiMock.send.mockReturnValue(of({ reply: 'Adding butter…', suggestions: [] }));
    fixture.detectChanges();
    tick();

    const onTranscript = voiceMock.startListeningWithFallback.mock.calls[0][1] as (text: string) => void;
    onTranscript('add butter to shopping list');
    tick();

    expect(chatApiMock.send).toHaveBeenCalledWith({
      message: 'add butter to shopping list',
      history: [],
    });
    expect(voiceMock.speak).toHaveBeenCalledWith('Adding butter…');
  }));

  it('should close ingredients panel by default', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    expect(component.ingredientsPanelOpen()).toBe(false);
  }));

  it('should open ingredients panel on toggleIngredientsPanel', fakeAsync(() => {
    setupTestBed(vi.fn().mockReturnValue(of(mockRecipe)));
    fixture.detectChanges();
    tick();

    component.toggleIngredientsPanel();

    expect(component.ingredientsPanelOpen()).toBe(true);
  }));
});
