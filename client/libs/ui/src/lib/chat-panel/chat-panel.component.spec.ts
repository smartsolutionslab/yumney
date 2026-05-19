import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { TranslocoService } from '@jsverse/transloco';
import { ChatPanelComponent } from './chat-panel.component';
import { RecipeApiService } from '@yumney/shared/api-recipes';
import { ChatApiService } from '@yumney/shared/chat-api';
import { ChatStateService, setupTranslocoTesting, VoiceService } from '@yumney/shared/models';
import { signal } from '@angular/core';

const en = {
  chat: {
    title: 'Recipe Chat',
    welcome: 'Ask me anything about recipes',
    examples: { 1: 'Quick meals', 2: 'Vegetarian', 3: 'Cookies' },
    placeholder: 'Ask…',
    inputLabel: 'Chat input',
    send: 'Send',
    close: 'Close',
    clear: 'Clear',
    clearHistory: 'Clear history',
    errors: { offline: 'Offline', failed: 'Failed' },
    actions: {
      navigate: 'Open',
      openShopping: 'Open shopping list',
      openMealPlanner: 'Open meal planner',
      openRecipes: 'Open recipes',
      openSettings: 'Open settings',
      openRecipe: 'Open recipe',
      startCooking: 'Start cooking',
    },
    mic: { start: 'Start voice input', stop: 'Stop voice input', listening: 'Listening...' },
    tts: { mute: 'Mute', unmute: 'Unmute', replay: 'Replay' },
  },
  commandBar: {
    hints: {
      default: 'Ask me anything...',
    },
  },
};

describe('ChatPanelComponent', () => {
  let fixture: ComponentFixture<ChatPanelComponent>;
  let chatState: ChatStateService;
  let chatApiMock: { send: ReturnType<typeof vi.fn> };
  let recipeApiMock: { importRecipe: ReturnType<typeof vi.fn>; importFromText: ReturnType<typeof vi.fn> };
  let voiceMock: {
    sttSupported: ReturnType<typeof signal<boolean>>;
    ttsSupported: ReturnType<typeof signal<boolean>>;
    isListening: ReturnType<typeof signal<boolean>>;
    isSpeaking: ReturnType<typeof signal<boolean>>;
    muted: ReturnType<typeof signal<boolean>>;
    setLanguage: ReturnType<typeof vi.fn>;
    setMuted: ReturnType<typeof vi.fn>;
    startListeningForTranscript: ReturnType<typeof vi.fn>;
    stopListening: ReturnType<typeof vi.fn>;
    speak: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    chatApiMock = {
      send: vi.fn().mockReturnValue(of({ reply: 'Hello!', suggestions: [] })),
    };
    recipeApiMock = {
      importRecipe: vi.fn().mockReturnValue(of({ title: 'Test', ingredients: [], steps: [] })),
      importFromText: vi.fn().mockReturnValue(of({ title: 'Test', ingredients: [], steps: [] })),
    };
    voiceMock = {
      sttSupported: signal(true),
      ttsSupported: signal(true),
      isListening: signal(false),
      isSpeaking: signal(false),
      muted: signal(false),
      setLanguage: vi.fn(),
      setMuted: vi.fn((value: boolean) => voiceMock.muted.set(value)),
      startListeningForTranscript: vi.fn(),
      stopListening: vi.fn(),
      speak: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ChatPanelComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: ChatApiService, useValue: chatApiMock },
        { provide: RecipeApiService, useValue: recipeApiMock },
        { provide: VoiceService, useValue: voiceMock },
        ChatStateService,
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatPanelComponent);
    chatState = TestBed.inject(ChatStateService);
  });

  afterEach(() => {
    chatState.clear();
    chatState.close();
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should not render panel when closed', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.chat-panel')).toBeFalsy();
  });

  it('should render panel when opened', () => {
    chatState.open();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.chat-panel')).toBeTruthy();
  });

  it('should show welcome message when no history', () => {
    chatState.open();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.chat-welcome')).toBeTruthy();
  });

  it('should send message and add user message to state', async () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(chatApiMock.send).toHaveBeenCalled();
    expect(chatState.messages().length).toBeGreaterThanOrEqual(1);
    expect(chatState.messages()[0]).toEqual({ role: 'user', content: 'Hello' });
  });

  it('should add assistant reply on successful response', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(of({ reply: 'Try pasta!', suggestions: [] }));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Suggest something');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(chatState.messages().length).toBe(2);
    expect(chatState.messages()[1]).toEqual({ role: 'assistant', content: 'Try pasta!' });
    expect(chatState.isThinking()).toBe(false);
  });

  it('should show error when request fails', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(throwError(() => new Error('fail')));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(fixture.componentInstance['error']()).toBe('chat.errors.failed');
    expect(chatState.isThinking()).toBe(false);
  });

  it('should not send empty message', () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('   ');
    fixture.componentInstance['onSend']();

    expect(chatApiMock.send).not.toHaveBeenCalled();
  });

  it('should clear input after sending', async () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(fixture.componentInstance['input']()).toBe('');
  });

  it('should close panel when close button clicked', () => {
    chatState.open();
    fixture.detectChanges();

    const closeBtn = fixture.nativeElement.querySelector('.chat-close');
    closeBtn.click();
    fixture.detectChanges();

    expect(chatState.isOpen()).toBe(false);
  });

  it('should clear messages when clear button clicked', async () => {
    chatState.open();
    chatState.addMessage({ role: 'user', content: 'Hello' });
    fixture.detectChanges();

    const clearBtn = fixture.nativeElement.querySelector('.chat-clear');
    clearBtn.click();
    fixture.detectChanges();

    expect(chatState.messages().length).toBe(0);
  });

  it('should send on Enter key without shift', () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    const event = new KeyboardEvent('keydown', { key: 'Enter' });
    fixture.componentInstance['onKeydown'](event);

    expect(chatApiMock.send).toHaveBeenCalled();
  });

  it('should not send on Shift+Enter (allow newline)', () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true });
    fixture.componentInstance['onKeydown'](event);

    expect(chatApiMock.send).not.toHaveBeenCalled();
  });

  it('should close panel when backdrop is clicked', () => {
    chatState.open();
    fixture.detectChanges();

    const backdrop = fixture.nativeElement.querySelector('.chat-backdrop');
    backdrop.click();
    fixture.detectChanges();

    expect(chatState.isOpen()).toBe(false);
  });

  it('should render backdrop when panel is open', () => {
    chatState.open();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.chat-backdrop')).toBeTruthy();
  });

  it('should show offline error when request fails with status 0', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(throwError(() => ({ status: 0 })));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(fixture.componentInstance['error']()).toBe('chat.errors.offline');
  });

  it('should show generic error when request fails with server error', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(throwError(() => ({ status: 500 })));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(fixture.componentInstance['error']()).toBe('chat.errors.failed');
  });

  it('should update lastAssistantMessage on successful response', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(of({ reply: 'Try pasta!', suggestions: [] }));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Suggest something');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(fixture.componentInstance['lastAssistantMessage']()).toBe('Try pasta!');
  });

  it('should render aria-live region with last assistant message', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(of({ reply: 'Try pasta!', suggestions: [] }));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Suggest');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    const liveRegion = fixture.nativeElement.querySelector('[role="status"][aria-live="polite"]');
    expect(liveRegion).toBeTruthy();
    expect(liveRegion.textContent.trim()).toBe('Try pasta!');
  });

  // ── URL / text detection ──

  it('should trigger URL import when input is a URL', async () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('https://example.com/recipe');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(recipeApiMock.importRecipe).toHaveBeenCalledWith({ url: 'https://example.com/recipe' });
    expect(chatApiMock.send).not.toHaveBeenCalled();
  });

  it('should trigger text import when input is long recipe text', async () => {
    chatState.open();
    fixture.detectChanges();

    const longText = 'Pasta Carbonara\n200g spaghetti\n4 eggs\n100g guanciale\nBoil pasta, mix with eggs and cheese';
    fixture.componentInstance['input'].set(longText);
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(recipeApiMock.importFromText).toHaveBeenCalledWith(longText);
    expect(chatApiMock.send).not.toHaveBeenCalled();
  });

  it('should send regular chat for short text input', async () => {
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('What can I cook?');
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(chatApiMock.send).toHaveBeenCalled();
    expect(recipeApiMock.importRecipe).not.toHaveBeenCalled();
    expect(recipeApiMock.importFromText).not.toHaveBeenCalled();
  });

  // URL / recipe-text detection lives in `ChatMessageDispatcher` and is covered
  // by chat-message-dispatcher.service.spec.ts.

  // ── Actions ──

  it('should render action buttons returned from chat response', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(
      of({
        reply: 'Sure!',
        suggestions: [],
        actions: [{ type: 'navigate', route: '/shopping' }],
      }),
    );
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Open shopping list');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('[data-testid="chat-action"]');
    expect(buttons.length).toBe(1);
    expect(buttons[0].textContent.trim()).toContain('Open shopping list');
  });

  it('should navigate and close panel when an action button is clicked', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    chatApiMock.send = vi.fn().mockReturnValue(
      of({
        reply: 'Sure!',
        suggestions: [],
        actions: [{ type: 'navigate', route: '/meal-planner' }],
      }),
    );
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Open meal planner');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('[data-testid="chat-action"]');
    button.click();

    expect(navigateSpy).toHaveBeenCalledWith(['/meal-planner']);
    expect(chatState.isOpen()).toBe(false);
  });

  it('should map openRecipe action to recipe detail route', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    chatApiMock.send = vi.fn().mockReturnValue(
      of({
        reply: 'Here it is.',
        suggestions: [],
        actions: [{ type: 'openRecipe', recipeIdentifier: 'abc-123' }],
      }),
    );
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Show me carbonara');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="chat-action"]').click();

    expect(navigateSpy).toHaveBeenCalledWith(['/recipes/abc-123']);
  });

  it('should map startCookMode action to cook route', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    chatApiMock.send = vi.fn().mockReturnValue(
      of({
        reply: "Let's go.",
        suggestions: [],
        actions: [{ type: 'startCookMode', recipeIdentifier: 'def-456' }],
      }),
    );
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Cook now');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="chat-action"]').click();

    expect(navigateSpy).toHaveBeenCalledWith(['/recipes/def-456/cook']);
  });

  it('should not render action container when actions are empty', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(of({ reply: 'Hi', suggestions: [], actions: [] }));
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Hello');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="chat-actions"]')).toBeFalsy();
  });

  it('should clear actions when conversation is cleared', async () => {
    chatApiMock.send = vi.fn().mockReturnValue(
      of({
        reply: 'Sure!',
        suggestions: [],
        actions: [{ type: 'navigate', route: '/shopping' }],
      }),
    );
    chatState.open();
    fixture.detectChanges();

    fixture.componentInstance['input'].set('Open shopping list');
    fixture.componentInstance['onSend']();
    await Promise.resolve();
    fixture.detectChanges();

    fixture.componentInstance['onClear']();
    fixture.detectChanges();

    expect(fixture.componentInstance['lastActions']().length).toBe(0);
  });

  describe('mic button (US-360)', () => {
    it('renders the mic button when speech recognition is supported', () => {
      chatState.open();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="chat-mic"]')).toBeTruthy();
    });

    it('hides the mic button when speech recognition is unsupported', () => {
      voiceMock.sttSupported.set(false);
      chatState.open();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="chat-mic"]')).toBeNull();
    });

    it('starts transcript listening on the first click', () => {
      chatState.open();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('[data-testid="chat-mic"]') as HTMLButtonElement;
      button.click();

      expect(voiceMock.setLanguage).toHaveBeenCalled();
      expect(voiceMock.startListeningForTranscript).toHaveBeenCalledTimes(1);
      expect(voiceMock.stopListening).not.toHaveBeenCalled();
    });

    it('stops listening when clicked again while listening', () => {
      chatState.open();
      fixture.detectChanges();

      voiceMock.isListening.set(true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('[data-testid="chat-mic"]') as HTMLButtonElement;
      button.click();

      expect(voiceMock.stopListening).toHaveBeenCalledTimes(1);
      expect(voiceMock.startListeningForTranscript).not.toHaveBeenCalled();
    });

    it('appends the transcript to the existing input', () => {
      chatState.open();
      fixture.detectChanges();
      fixture.componentInstance['input'].set('Plan');

      fixture.componentInstance['onToggleMic']();
      const onTranscript = voiceMock.startListeningForTranscript.mock.calls[0][0] as (text: string) => void;
      onTranscript('the week');

      expect(fixture.componentInstance['input']()).toBe('Plan the week');
    });

    it('sets recognition language to the active translation', () => {
      chatState.open();
      fixture.detectChanges();
      fixture.componentInstance['onToggleMic']();

      expect(voiceMock.setLanguage).toHaveBeenCalledWith(expect.stringMatching(/^(en|de)$/));
    });

    it('applies the listening class while listening', () => {
      chatState.open();
      voiceMock.isListening.set(true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('[data-testid="chat-mic"]');
      expect(button.classList.contains('listening')).toBe(true);
    });

    it('surfaces the noSpeech hint when recognition ends with nothing captured (TC-360-05)', () => {
      chatState.open();
      fixture.detectChanges();
      fixture.componentInstance['onToggleMic']();
      const onNoSpeech = voiceMock.startListeningForTranscript.mock.calls[0][1] as () => void;

      onNoSpeech();
      fixture.detectChanges();

      expect(fixture.componentInstance['error']()).toBe('chat.errors.noSpeech');
    });

    it('clears the prior noSpeech hint when a new transcript is captured', () => {
      chatState.open();
      fixture.detectChanges();
      // First mic press — silence.
      fixture.componentInstance['onToggleMic']();
      const firstNoSpeech = voiceMock.startListeningForTranscript.mock.calls[0][1] as () => void;
      firstNoSpeech();
      fixture.detectChanges();
      expect(fixture.componentInstance['error']()).toBe('chat.errors.noSpeech');

      // Second mic press — user speaks. The transcript path clears the prior hint.
      voiceMock.isListening.set(false);
      fixture.componentInstance['onToggleMic']();
      const secondTranscript = voiceMock.startListeningForTranscript.mock.calls[1][0] as (text: string) => void;
      secondTranscript('add milk');

      expect(fixture.componentInstance['error']()).toBeNull();
    });

    it('clears any prior error when starting a fresh mic session', () => {
      chatState.open();
      fixture.detectChanges();
      fixture.componentInstance['error'].set('chat.errors.offline');

      fixture.componentInstance['onToggleMic']();

      expect(fixture.componentInstance['error']()).toBeNull();
    });
  });

  describe('tts (US-361)', () => {
    async function sendMessage(content: string): Promise<void> {
      fixture.componentInstance['input'].set(content);
      fixture.componentInstance['onSend']();
      await Promise.resolve();
      fixture.detectChanges();
    }

    it('speaks the chat reply when the message was entered via voice', async () => {
      chatState.open();
      fixture.detectChanges();

      // Simulate the mic callback firing — it sets lastInputWasVoice=true.
      fixture.componentInstance['onToggleMic']();
      const onTranscript = voiceMock.startListeningForTranscript.mock.calls[0][0] as (text: string) => void;
      onTranscript('Add milk');

      chatApiMock.send.mockReturnValue(of({ reply: 'Added 1 liter milk', suggestions: [] }));
      await sendMessage('Add milk');

      expect(voiceMock.speak).toHaveBeenCalledWith('Added 1 liter milk');
    });

    it('does not speak the reply when the message was typed', async () => {
      chatState.open();
      fixture.detectChanges();

      chatApiMock.send.mockReturnValue(of({ reply: 'Sure!', suggestions: [] }));
      await sendMessage('Plan my week');

      expect(voiceMock.speak).not.toHaveBeenCalled();
    });

    it('resets the voice flag after the reply, so the next typed message stays silent', async () => {
      chatState.open();
      fixture.detectChanges();

      fixture.componentInstance['onToggleMic']();
      const onTranscript = voiceMock.startListeningForTranscript.mock.calls[0][0] as (text: string) => void;
      onTranscript('Add milk');

      chatApiMock.send.mockReturnValue(of({ reply: 'Added', suggestions: [] }));
      await sendMessage('Add milk');
      voiceMock.speak.mockClear();

      chatApiMock.send.mockReturnValue(of({ reply: 'OK', suggestions: [] }));
      await sendMessage('Plan my week');

      expect(voiceMock.speak).not.toHaveBeenCalled();
    });

    it('typing replaces voice mode (no TTS even after a mic transcript)', async () => {
      chatState.open();
      fixture.detectChanges();

      fixture.componentInstance['onToggleMic']();
      const onTranscript = voiceMock.startListeningForTranscript.mock.calls[0][0] as (text: string) => void;
      onTranscript('Add milk');

      // Simulate user typing — ngModelChange path through onInputChange.
      fixture.componentInstance['onInputChange']('Add milk and bread');

      chatApiMock.send.mockReturnValue(of({ reply: 'Added', suggestions: [] }));
      await sendMessage('Add milk and bread');

      expect(voiceMock.speak).not.toHaveBeenCalled();
    });

    it('renders the mute toggle when TTS is supported', () => {
      chatState.open();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="chat-mute"]')).toBeTruthy();
    });

    it('hides the mute toggle when TTS is unsupported', () => {
      voiceMock.ttsSupported.set(false);
      chatState.open();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="chat-mute"]')).toBeNull();
    });

    it('mute click flips voice.muted', () => {
      chatState.open();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('[data-testid="chat-mute"]') as HTMLButtonElement;
      button.click();

      expect(voiceMock.setMuted).toHaveBeenCalledWith(true);
    });

    it('replay button is hidden until there is a last assistant message', () => {
      chatState.open();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="chat-replay"]')).toBeNull();
    });

    it('replay button speaks the last assistant message', async () => {
      chatState.open();
      fixture.detectChanges();
      chatApiMock.send.mockReturnValue(of({ reply: 'Hello there', suggestions: [] }));
      await sendMessage('Hi');
      voiceMock.speak.mockClear();

      const button = fixture.nativeElement.querySelector('[data-testid="chat-replay"]') as HTMLButtonElement;
      button.click();

      expect(voiceMock.speak).toHaveBeenCalledWith('Hello there');
    });

    it('replay button is disabled while muted', async () => {
      chatState.open();
      chatApiMock.send.mockReturnValue(of({ reply: 'Hello', suggestions: [] }));
      await sendMessage('Hi');

      voiceMock.muted.set(true);
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('[data-testid="chat-replay"]') as HTMLButtonElement;
      expect(button.disabled).toBe(true);
    });

    it('propagates the active Transloco language to voice.setLanguage before each speak (TC-361-05)', async () => {
      chatState.open();
      fixture.detectChanges();

      fixture.componentInstance['onToggleMic']();
      const onTranscript = voiceMock.startListeningForTranscript.mock.calls[0][0] as (text: string) => void;
      onTranscript('Was gibt es Dienstag?');

      // The default test Transloco lang is 'en'; assert that the value flowing
      // into setLanguage is the active lang, not a hard-coded constant. We can
      // change the active lang at runtime via TranslocoService.setActiveLang
      // and the next speak path should pick the new value up.
      const transloco = TestBed.inject(TranslocoService);
      // setupTranslocoTesting is configured with availableLangs=['en']; expand
      // here so 'de' is acceptable.
      transloco.setAvailableLangs(['en', 'de']);
      transloco.setActiveLang('de');

      chatApiMock.send.mockReturnValue(of({ reply: 'Dienstag ist Spaghetti', suggestions: [] }));
      await sendMessage('Was gibt es Dienstag?');

      expect(voiceMock.setLanguage).toHaveBeenLastCalledWith('de');
      expect(voiceMock.speak).toHaveBeenCalledWith('Dienstag ist Spaghetti');
    });

    it('replay reapplies the active Transloco language to voice before speaking', async () => {
      chatState.open();
      chatApiMock.send.mockReturnValue(of({ reply: 'Hallo', suggestions: [] }));
      await sendMessage('Hi');
      voiceMock.setLanguage.mockClear();

      const transloco = TestBed.inject(TranslocoService);
      transloco.setAvailableLangs(['en', 'de']);
      transloco.setActiveLang('de');

      const button = fixture.nativeElement.querySelector('[data-testid="chat-replay"]') as HTMLButtonElement;
      button.click();

      expect(voiceMock.setLanguage).toHaveBeenCalledWith('de');
    });
  });
});
