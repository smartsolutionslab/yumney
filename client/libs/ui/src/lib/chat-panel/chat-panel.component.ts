import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  ElementRef,
  inject,
  signal,
  viewChild,
  effect,
  AfterViewInit,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { ChatApiService, type ChatRecipeSuggestion } from '@yumney/shared/api-client';
import { ChatHintService, ChatStateService, ROUTES } from '@yumney/shared/models';

@Component({
  selector: 'yn-chat-panel',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoModule, LucideAngularModule],
  templateUrl: './chat-panel.component.html',
  styleUrl: './chat-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatPanelComponent implements AfterViewInit {
  protected readonly ROUTES = ROUTES;

  protected state = inject(ChatStateService);
  protected hints = inject(ChatHintService);
  private chatApi = inject(ChatApiService);
  private destroyRef = inject(DestroyRef);

  protected input = signal('');
  protected error = signal<string | null>(null);
  protected lastSuggestions = signal<ChatRecipeSuggestion[]>([]);
  protected lastAssistantMessage = signal('');

  messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messages');

  constructor() {
    effect(() => {
      this.state.messages();
      this.state.isThinking();
      queueMicrotask(() => this.scrollToBottom());
    });
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
  }

  protected onClose(): void {
    this.state.close();
  }

  protected onBackdropClick(): void {
    this.state.close();
  }

  protected onSend(): void {
    const message = this.input().trim();
    if (!message || this.state.isThinking()) return;

    if (!navigator.onLine) {
      this.error.set('chat.errors.offline');
      return;
    }

    this.error.set(null);
    this.input.set('');
    this.state.addMessage({ role: 'user', content: message });
    this.state.setThinking(true);

    const history = this.state.messages().slice(0, -1);

    this.chatApi
      .send({ message, history })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.state.addMessage({ role: 'assistant', content: response.reply });
          this.lastSuggestions.set(response.suggestions);
          this.lastAssistantMessage.set(response.reply);
          this.state.setThinking(false);
        },
        error: (err) => {
          this.state.setThinking(false);
          if (err?.status === 0 || !navigator.onLine) {
            this.error.set('chat.errors.offline');
          } else {
            this.error.set('chat.errors.failed');
          }
        },
      });
  }

  protected onClear(): void {
    this.state.clear();
    this.lastSuggestions.set([]);
    this.lastAssistantMessage.set('');
    this.error.set(null);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.onSend();
    }
  }

  protected onSuggestionClick(): void {
    this.state.close();
  }

  private scrollToBottom(): void {
    const el = this.messagesContainer()?.nativeElement;
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }
}
