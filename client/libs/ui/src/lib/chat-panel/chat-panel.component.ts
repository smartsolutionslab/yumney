import {
  Component,
  ChangeDetectionStrategy,
  ElementRef,
  inject,
  signal,
  viewChild,
  effect,
  AfterViewInit,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { ChatApiService, type ChatRecipeSuggestion } from '@yumney/shared/api-client';
import { ChatStateService, ROUTES } from '@yumney/shared/models';

@Component({
  selector: 'yn-chat-panel',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoModule],
  templateUrl: './chat-panel.component.html',
  styleUrl: './chat-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatPanelComponent implements AfterViewInit {
  protected readonly ROUTES = ROUTES;

  protected state = inject(ChatStateService);
  private chatApi = inject(ChatApiService);

  protected input = signal('');
  protected error = signal<string | null>(null);
  protected lastSuggestions = signal<ChatRecipeSuggestion[]>([]);

  messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messages');

  constructor() {
    // Auto-scroll on new messages
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

    this.chatApi.send({ message, history }).subscribe({
      next: (response) => {
        this.state.addMessage({ role: 'assistant', content: response.reply });
        this.lastSuggestions.set(response.suggestions);
        this.state.setThinking(false);
      },
      error: () => {
        this.state.setThinking(false);
        this.error.set('chat.errors.failed');
      },
    });
  }

  protected onClear(): void {
    this.state.clear();
    this.lastSuggestions.set([]);
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
