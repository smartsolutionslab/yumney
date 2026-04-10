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
import {
  ChatApiService,
  RecipeApiService,
  type ChatRecipeSuggestion,
} from '@yumney/shared/api-client';
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
  private recipeApi = inject(RecipeApiService);
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

    if (this.looksLikeUrl(message)) {
      this.handleUrlImport(message);
    } else if (this.looksLikeRecipeText(message)) {
      this.handleTextImport(message);
    } else {
      this.handleChat(message);
    }
  }

  private handleChat(message: string): void {
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
        error: (err) => this.handleError(err),
      });
  }

  private handleUrlImport(url: string): void {
    this.recipeApi
      .importRecipe({ url })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          const reply = `I found a recipe: **${recipe.title}**\n${recipe.ingredients.length} ingredients, ${recipe.steps.length} steps.`;
          this.state.addMessage({ role: 'assistant', content: reply });
          this.lastSuggestions.set([
            { recipeIdentifier: null, title: recipe.title, reason: 'Extracted from URL' },
          ]);
          this.lastAssistantMessage.set(reply);
          this.state.setThinking(false);
        },
        error: (err) => this.handleError(err),
      });
  }

  private handleTextImport(text: string): void {
    this.chatApi
      .importFromText(text)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recipe) => {
          const reply = `I found a recipe: **${recipe.title}**\n${recipe.ingredients.length} ingredients, ${recipe.steps.length} steps.`;
          this.state.addMessage({ role: 'assistant', content: reply });
          this.lastSuggestions.set([
            { recipeIdentifier: null, title: recipe.title, reason: 'Extracted from text' },
          ]);
          this.lastAssistantMessage.set(reply);
          this.state.setThinking(false);
        },
        error: (err) => this.handleError(err),
      });
  }

  private handleError(err: { status?: number }): void {
    this.state.setThinking(false);
    if (err?.status === 0 || !navigator.onLine) {
      this.error.set('chat.errors.offline');
    } else {
      this.error.set('chat.errors.failed');
    }
  }

  private looksLikeUrl(text: string): boolean {
    return /^https?:\/\/\S+$/i.test(text);
  }

  private looksLikeRecipeText(text: string): boolean {
    const lines = text.split('\n').length;
    const words = text.split(/\s+/).length;
    return lines >= 3 || words >= 30;
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
