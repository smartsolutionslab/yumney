import { DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoService } from '@jsverse/transloco';
import { of, throwError } from 'rxjs';
import { ChatMessageDispatcher, CookingTimerService, VoiceService, type VoiceCommand } from '@yumney/shared/models';
import { CookModeVoiceController, type CookVoiceActions } from './cook-mode-voice.controller';

describe('CookModeVoiceController', () => {
  let controller: CookModeVoiceController;
  let voice: {
    startListeningWithFallback: ReturnType<typeof vi.fn>;
    stopSpeaking: ReturnType<typeof vi.fn>;
    speak: ReturnType<typeof vi.fn>;
  };
  let timers: { cancelAll: ReturnType<typeof vi.fn>; start: ReturnType<typeof vi.fn> };
  let transloco: { translate: ReturnType<typeof vi.fn> };
  let dispatcher: { dispatch: ReturnType<typeof vi.fn> };
  let actions: { [K in keyof CookVoiceActions]: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    voice = { startListeningWithFallback: vi.fn(), stopSpeaking: vi.fn(), speak: vi.fn() };
    timers = { cancelAll: vi.fn(), start: vi.fn() };
    transloco = { translate: vi.fn().mockReturnValue('Timer') };
    dispatcher = { dispatch: vi.fn().mockReturnValue(of({ reply: 'r', suggestions: [], actions: [] })) };
    actions = { next: vi.fn(), previous: vi.fn(), repeat: vi.fn(), stop: vi.fn(), showIngredients: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        CookModeVoiceController,
        { provide: VoiceService, useValue: voice },
        { provide: CookingTimerService, useValue: timers },
        { provide: TranslocoService, useValue: transloco },
        { provide: ChatMessageDispatcher, useValue: dispatcher },
        { provide: DestroyRef, useValue: { destroyed: false, onDestroy: () => () => undefined } },
      ],
    });

    controller = TestBed.inject(CookModeVoiceController);
  });

  function fireCommand(command: VoiceCommand): void {
    controller.start(actions);
    const onCommand = voice.startListeningWithFallback.mock.calls[0][0] as (cmd: VoiceCommand) => void;
    onCommand(command);
  }

  function fireTranscript(text: string): void {
    controller.start(actions);
    const onTranscript = voice.startListeningWithFallback.mock.calls[0][1] as (text: string) => void;
    onTranscript(text);
  }

  it('routes "next" command to actions.next', () => {
    fireCommand({ type: 'next' });
    expect(actions.next).toHaveBeenCalled();
  });

  it('routes "previous" command to actions.previous', () => {
    fireCommand({ type: 'previous' });
    expect(actions.previous).toHaveBeenCalled();
  });

  it('routes "repeat" command to actions.repeat', () => {
    fireCommand({ type: 'repeat' });
    expect(actions.repeat).toHaveBeenCalled();
  });

  it('routes "ingredients" command to actions.showIngredients', () => {
    fireCommand({ type: 'ingredients' });
    expect(actions.showIngredients).toHaveBeenCalled();
  });

  it('on "stop" cancels timers, stops TTS, and notifies actions', () => {
    fireCommand({ type: 'stop' });
    expect(voice.stopSpeaking).toHaveBeenCalled();
    expect(timers.cancelAll).toHaveBeenCalled();
    expect(actions.stop).toHaveBeenCalled();
  });

  it('on "timer" starts a timer with the localized default name', () => {
    fireCommand({ type: 'timer', minutes: 5 });
    expect(transloco.translate).toHaveBeenCalledWith('recipes.cook.timer.defaultName');
    expect(timers.start).toHaveBeenCalledWith('Timer', 5);
  });

  it('routes transcripts through the chat dispatcher and speaks the reply', () => {
    dispatcher.dispatch.mockReturnValue(of({ reply: 'Adding butter…', suggestions: [], actions: [] }));
    fireTranscript('add butter to shopping list');

    expect(dispatcher.dispatch).toHaveBeenCalledWith('add butter to shopping list', []);
    expect(voice.speak).toHaveBeenCalledWith('Adding butter…');
  });

  it('swallows dispatcher errors silently', () => {
    dispatcher.dispatch.mockReturnValue(throwError(() => new Error('boom')));
    expect(() => fireTranscript('anything')).not.toThrow();
  });
});
