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
    // Press Enter to submit the form rather than clicking the button.
    // Force-clicking bypassed the panel's slide-in animation stability
    // check but didn't reliably reach the (ngSubmit) handler — Enter
    // triggers form submission via the browser's native path.
    await this.input.press('Enter');
  }

  userMessage(index = 0): Locator {
    return this.page.locator(SELECTORS.chat.userMessage).nth(index);
  }

  assistantMessage(index = 0): Locator {
    return this.page.locator(SELECTORS.chat.assistantMessage).nth(index);
  }
}
