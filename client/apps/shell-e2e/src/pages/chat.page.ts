import { type Page, type Locator } from '@playwright/test';

export class ChatPage {
  readonly fab: Locator;
  readonly panel: Locator;
  readonly backdrop: Locator;
  readonly closeButton: Locator;
  readonly clearButton: Locator;
  readonly sendButton: Locator;
  readonly input: Locator;
  readonly welcome: Locator;
  readonly examples: Locator;
  private readonly userMessages: Locator;
  private readonly assistantMessages: Locator;

  constructor(private page: Page) {
    this.fab = page.locator('[data-testid="chat-fab"]');
    this.panel = page.locator('[data-testid="chat-panel"]');
    this.backdrop = page.locator('[data-testid="chat-backdrop"]');
    this.closeButton = page.locator('[data-testid="chat-close"]');
    this.clearButton = page.locator('[data-testid="chat-clear"]');
    this.sendButton = page.locator('[data-testid="chat-send"]');
    this.input = page.locator('[data-testid="chat-input"]');
    this.welcome = page.locator('[data-testid="chat-welcome"]');
    // Multi-element / state-based — each li is a separate item, role-based
    // bubbles distinguish user vs assistant.
    this.examples = page.locator('.chat-examples li');
    this.userMessages = page.locator('.chat-message.role-user .chat-bubble');
    this.assistantMessages = page.locator('.chat-message.role-assistant .chat-bubble');
  }

  async open(): Promise<void> {
    await this.fab.click();
  }

  async close(): Promise<void> {
    await this.closeButton.click();
  }

  async sendMessage(text: string): Promise<void> {
    await this.input.fill(text);
    // Press Enter to submit the form rather than clicking the button.
    // Force-clicking bypassed the panel's slide-in animation stability
    // check but didn't reliably reach the (ngSubmit) handler — Enter
    // triggers form submission via the browser's native path.
    await this.input.press('Enter');
  }

  userMessage(index = 0): Locator {
    return this.userMessages.nth(index);
  }

  assistantMessage(index = 0): Locator {
    return this.assistantMessages.nth(index);
  }
}
