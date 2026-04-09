import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ChatPanelComponent } from './chat-panel.component';
import { ChatApiService } from '@yumney/shared/api-client';
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
  },
};

describe('ChatPanelComponent', () => {
  let fixture: ComponentFixture<ChatPanelComponent>;
  let chatState: ChatStateService;
  let chatApiMock: { send: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    chatApiMock = {
      send: vi.fn().mockReturnValue(of({ reply: 'Hello!', suggestions: [] })),
    };

    await TestBed.configureTestingModule({
      imports: [ChatPanelComponent, setupTranslocoTesting(en)],
      providers: [
        provideRouter([]),
        { provide: ChatApiService, useValue: chatApiMock },
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
});
