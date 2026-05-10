import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ChatPanelComponent } from './chat-panel.component';
import { ChatApiService, RecipeApiService } from '@yumney/shared/api-client';
import { ChatStateService, setupTranslocoTesting } from '@yumney/shared/models';

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
  let chatApiMock: { send: ReturnType<typeof vi.fn>; importFromText: ReturnType<typeof vi.fn> };
  let recipeApiMock: { importRecipe: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    chatApiMock = {
      send: vi.fn().mockReturnValue(of({ reply: 'Hello!', suggestions: [] })),
      importFromText: vi.fn().mockReturnValue(of({ title: 'Test', ingredients: [], steps: [] })),
    };
    recipeApiMock = {
      importRecipe: vi.fn().mockReturnValue(of({ title: 'Test', ingredients: [], steps: [] })),
    };

    await TestBed.configureTestingModule({
      imports: [ChatPanelComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: ChatApiService, useValue: chatApiMock },
        { provide: RecipeApiService, useValue: recipeApiMock },
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

    const longText =
      'Pasta Carbonara\n200g spaghetti\n4 eggs\n100g guanciale\nBoil pasta, mix with eggs and cheese';
    fixture.componentInstance['input'].set(longText);
    fixture.componentInstance['onSend']();
    await Promise.resolve();

    expect(chatApiMock.importFromText).toHaveBeenCalledWith(longText);
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
    expect(chatApiMock.importFromText).not.toHaveBeenCalled();
  });

  it('should detect URL correctly', () => {
    expect(fixture.componentInstance['looksLikeUrl']('https://example.com/recipe')).toBe(true);
    expect(fixture.componentInstance['looksLikeUrl']('http://food.com/pasta')).toBe(true);
    expect(fixture.componentInstance['looksLikeUrl']('not a url')).toBe(false);
    expect(fixture.componentInstance['looksLikeUrl']('Check https://example.com')).toBe(false);
  });

  it('should detect recipe text correctly', () => {
    expect(fixture.componentInstance['looksLikeRecipeText']('line1\nline2\nline3')).toBe(true);
    expect(fixture.componentInstance['looksLikeRecipeText']('a '.repeat(30))).toBe(true);
    expect(fixture.componentInstance['looksLikeRecipeText']('short text')).toBe(false);
  });

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
});
