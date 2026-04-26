import { type Page, type Locator } from '@playwright/test';
import { SELECTORS } from '../helpers/selectors';

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

  constructor(private page: Page) {
    this.fab = page.locator(SELECTORS.chat.fab);
    this.panel = page.locator(SELECTORS.chat.panel);
    this.backdrop = page.locator(SELECTORS.chat.backdrop);
    this.closeButton = page.locator(SELECTORS.chat.close);
    this.clearButton = page.locator(SELECTORS.chat.clear);
    this.sendButton = page.locator(SELECTORS.chat.send);
    this.input = page.locator(SELECTORS.chat.input);
    this.welcome = page.locator(SELECTORS.chat.welcome);
    this.examples = page.locator(SELECTORS.chat.examples);
  }

  async open(): Promise<void> {
    await this.fab.click();
  }

  async close(): Promise<void> {
    await this.closeButton.click();
  }

  async sendMessage(text: string): Promise<void> {
    await this.input.fill(text);
    // Chat panel slides in with a CSS transition; the send button can be
    // mid-animation when Playwright tries to click and triggers
    // "element is not stable" retries until test timeout. Force the click.
    await this.sendButton.click({ force: true });
  }

  userMessage(index = 0): Locator {
    return this.page.locator(SELECTORS.chat.userMessage).nth(index);
  }

  assistantMessage(index = 0): Locator {
    return this.page.locator(SELECTORS.chat.assistantMessage).nth(index);
  }
}
