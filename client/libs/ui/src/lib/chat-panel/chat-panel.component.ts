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
import { Router, RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import {
  ChatApiService,
  RecipeApiService,
  type ChatAction,
  type ChatRecipeSuggestion,
} from '@yumney/shared/api-client';
import { ChatHintService, ChatStateService, ROUTES, VoiceService } from '@yumney/shared/models';

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
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  protected voice = inject(VoiceService);
  private transloco = inject(TranslocoService);

  protected input = signal('');
  protected error = signal<string | null>(null);
  protected lastSuggestions = signal<ChatRecipeSuggestion[]>([]);
  protected lastActions = signal<ChatAction[]>([]);
  protected lastAssistantMessage = signal('');

  // Tracks whether the *currently pending* send originated from the mic so we
  // know whether to read the assistant reply aloud (US-361). Reset on typed
  // input / new mic capture, and re-checked in the chat response handler.
  protected lastInputWasVoice = signal(false);

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

  protected onInputChange(value: string): void {
    this.input.set(value);
    // Keyboard edits switch the conversation back to a "typed" interaction,
    // so the assistant reply won't be read aloud unless the user uses the
    // mic again.
    this.lastInputWasVoice.set(false);
  }

  protected onBackdropClick(): void {
    this.state.close();
  }

  protected onToggleMic(): void {
    if (this.voice.isListening()) {
      this.voice.stopListening();
      return;
    }
    this.voice.setLanguage(this.transloco.getActiveLang() as 'en' | 'de');
    this.voice.startListeningForTranscript((transcript) => {
      const current = this.input();
      this.input.set(current ? `${current} ${transcript}`.trim() : transcript);
      this.lastInputWasVoice.set(true);
    });
  }

  protected onToggleMute(): void {
    this.voice.setMuted(!this.voice.muted());
  }

  protected onReplayLast(): void {
    const reply = this.lastAssistantMessage();
    if (!reply) return;
    this.voice.setLanguage(this.transloco.getActiveLang() as 'en' | 'de');
    this.voice.speak(reply);
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
    const speakReply = this.lastInputWasVoice();

    this.chatApi
      .send({ message, history })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.state.addMessage({ role: 'assistant', content: response.reply });
          this.lastSuggestions.set(response.suggestions);
          this.lastActions.set(response.actions ?? []);
          this.lastAssistantMessage.set(response.reply);
          this.state.setThinking(false);
          if (speakReply) {
            this.voice.setLanguage(this.transloco.getActiveLang() as 'en' | 'de');
            this.voice.speak(response.reply);
          }
          this.lastInputWasVoice.set(false);
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
          const reply = this.buildRecipeReply(recipe);
          this.state.addMessage({ role: 'assistant', content: reply });
          this.lastSuggestions.set([
            {
              recipeIdentifier: null,
              title: recipe.title,
              reason: 'Extracted from URL',
            },
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
          const reply = this.buildRecipeReply(recipe);
          this.state.addMessage({ role: 'assistant', content: reply });
          this.lastSuggestions.set([
            {
              recipeIdentifier: null,
              title: recipe.title,
              reason: 'Extracted from text',
            },
          ]);
          this.lastAssistantMessage.set(reply);
          this.state.setThinking(false);
        },
        error: (err) => this.handleError(err),
      });
  }

  private buildRecipeReply(recipe: {
    title: string;
    ingredients: unknown[];
    steps: unknown[];
  }): string {
    const count = recipe.ingredients.length;
    const steps = recipe.steps.length;
    return `I found a recipe: **${recipe.title}**\n` + `${count} ingredients, ${steps} steps.`;
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
    this.lastActions.set([]);
    this.lastAssistantMessage.set('');
    this.error.set(null);
  }

  protected onActionClick(action: ChatAction): void {
    const route = this.routeForAction(action);
    if (!route) return;
    void this.router.navigate([route]);
    this.state.close();
  }

  protected actionLabelKey(action: ChatAction): string {
    if (action.type === 'openRecipe') return 'chat.actions.openRecipe';
    if (action.type === 'startCookMode') return 'chat.actions.startCooking';
    switch (action.route) {
      case '/shopping':
        return 'chat.actions.openShopping';
      case '/meal-planner':
        return 'chat.actions.openMealPlanner';
      case '/recipes':
        return 'chat.actions.openRecipes';
      case '/account':
        return 'chat.actions.openSettings';
      default:
        return 'chat.actions.navigate';
    }
  }

  private routeForAction(action: ChatAction): string | null {
    if (action.type === 'navigate') return action.route ?? null;
    if (!action.recipeIdentifier) return null;
    if (action.type === 'openRecipe') return ROUTES.recipes.detail(action.recipeIdentifier);
    if (action.type === 'startCookMode') return ROUTES.recipes.cook(action.recipeIdentifier);
    return null;
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
