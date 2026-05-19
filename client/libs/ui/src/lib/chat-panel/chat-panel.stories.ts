import { Component, inject, input } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { applicationConfig, Meta, moduleMetadata, StoryObj } from '@storybook/angular';
import { of } from 'rxjs';
import { RecipeApiService } from '@yumney/shared/api-recipes';
import { ChatApiService, type ChatMessage } from '@yumney/shared/chat-api';
import { ChatStateService } from '@yumney/shared/models';
import { provideYumneyIcons } from '../icons/provide-icons';
import { ChatPanelComponent } from './chat-panel.component';

const en = {
  chat: {
    title: 'Recipe Chat',
    welcome: 'Ask me anything about recipes',
    examples: {
      1: 'Quick meals',
      2: 'Vegetarian',
      3: 'Cookies',
    },
    placeholder: 'Ask…',
    inputLabel: 'Chat input',
    send: 'Send',
    close: 'Close',
    clear: 'Clear',
    clearHistory: 'Clear history',
    errors: { offline: "You're offline", failed: 'Something went wrong' },
  },
  commandBar: {
    hints: {
      default: 'Ask me anything...',
    },
  },
};

const chatApiStub = {
  send: () => of({ reply: '', suggestions: [] }),
  importFromText: () => of({ title: '', ingredients: [], steps: [] }),
};
const recipeApiStub = {
  importRecipe: () => of({ title: '', ingredients: [], steps: [] }),
};

// Host wrapper — seeds ChatStateService inside a DI context before the panel
// renders, so each story gets a fresh, deterministic state.
@Component({
  selector: 'yn-chat-panel-story-host',
  standalone: true,
  imports: [ChatPanelComponent],
  template: '<yn-chat-panel />',
})
class ChatPanelStoryHost {
  readonly messages = input<ChatMessage[]>([]);
  readonly thinking = input(false);
  private state = inject(ChatStateService);
  private transloco = inject(TranslocoService);

  constructor() {
    this.transloco.setActiveLang('en');
    this.state.clear();
    this.state.setThinking(false);
    // Seeding happens in a microtask so the signal inputs have been applied.
    queueMicrotask(() => {
      this.messages().forEach((m) => this.state.addMessage(m));
      this.state.setThinking(this.thinking());
      this.state.open();
    });
  }
}

const meta: Meta<ChatPanelStoryHost> = {
  title: 'Components/Chat Panel',
  component: ChatPanelStoryHost,
  decorators: [
    applicationConfig({
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        provideTransloco({
          config: {
            availableLangs: ['en'],
            defaultLang: 'en',
            fallbackLang: 'en',
            reRenderOnLangChange: false,
            prodMode: true,
          },
          loader: { getTranslation: () => of(en) },
        }),
      ],
    }),
    moduleMetadata({
      providers: [
        { provide: ChatApiService, useValue: chatApiStub },
        { provide: RecipeApiService, useValue: recipeApiStub },
      ],
    }),
  ],
};

export default meta;
type Story = StoryObj<ChatPanelStoryHost>;

export const Welcome: Story = {
  args: { messages: [] },
};

export const Conversation: Story = {
  args: {
    messages: [
      { role: 'user', content: 'Something quick for dinner?' },
      {
        role: 'assistant',
        content: 'How about a one-pan pasta? Pantry ingredients, 20 minutes.',
      },
      { role: 'user', content: 'Vegetarian version?' },
      {
        role: 'assistant',
        content: 'Sure — swap the pancetta for sautéed mushrooms and add a splash of lemon.',
      },
    ],
  },
};

export const Thinking: Story = {
  args: {
    thinking: true,
    messages: [{ role: 'user', content: 'Dessert ideas?' }],
  },
};
