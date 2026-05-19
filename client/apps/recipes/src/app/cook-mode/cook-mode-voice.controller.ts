import { DestroyRef, Injectable, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslocoService } from '@jsverse/transloco';
import { ChatMessageDispatcher, CookingTimerService, VoiceService, type VoiceCommand } from '@yumney/shared/models';

export interface CookVoiceActions {
  next: () => void;
  previous: () => void;
  repeat: () => void;
  stop: () => void;
  showIngredients: () => void;
}

@Injectable()
export class CookModeVoiceController {
  private voice = inject(VoiceService);
  private timers = inject(CookingTimerService);
  private transloco = inject(TranslocoService);
  private dispatcher = inject(ChatMessageDispatcher);
  private destroyRef = inject(DestroyRef);

  start(actions: CookVoiceActions): void {
    this.voice.startListeningWithFallback(
      (command) => this.handleCommand(command, actions),
      (transcript) => this.handleTranscript(transcript),
    );
  }

  private handleCommand(command: VoiceCommand, actions: CookVoiceActions): void {
    switch (command.type) {
      case 'next':
        actions.next();
        break;
      case 'previous':
        actions.previous();
        break;
      case 'repeat':
        actions.repeat();
        break;
      case 'stop':
        this.voice.stopSpeaking();
        this.timers.cancelAll();
        actions.stop();
        break;
      case 'ingredients':
        actions.showIngredients();
        break;
      case 'timer': {
        const name = this.transloco.translate('recipes.cook.timer.defaultName');
        this.timers.start(name, command.minutes);
        break;
      }
    }
  }

  private handleTranscript(transcript: string): void {
    // Route through the chat dispatcher so users can say things like
    // "add butter to shopping list" while cooking without leaving the screen.
    // The reply rides the same TTS path as auto-read steps.
    this.dispatcher
      .dispatch(transcript, [])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.voice.speak(result.reply),
        error: () => undefined,
      });
  }
}
