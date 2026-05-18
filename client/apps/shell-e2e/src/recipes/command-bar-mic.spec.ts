import { test, expect } from '../fixtures/auth.fixture';
import { ChatPage } from '../pages/chat.page';
import { TIMEOUTS } from '../helpers/timeouts';

// US-360: Push-to-Talk Voice Input on the Command Bar.
// The mic button lives inside the chat panel's input area (mounted globally
// via yn-app-layout and toggled by the persistent command FAB).
// Browsers Playwright runs against don't expose a real SpeechRecognition,
// so each test injects a mock constructor via addInitScript that records
// start() / stop() and exposes the captured event handlers on window so the
// test can drive onresult / onerror / onend deterministically.
test.describe('Command bar mic (US-360)', () => {
  const installMockRecognition = async (page: import('@playwright/test').Page): Promise<void> => {
    await page.addInitScript(() => {
      type RecogHandlers = {
        onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
        onerror: ((event: { error: string }) => void) | null;
        onend: (() => void) | null;
      };
      const created: Array<RecogHandlers & { lang: string; continuous: boolean; interimResults: boolean; started: boolean }> = [];
      class MockRecognition {
        lang = '';
        continuous = false;
        interimResults = false;
        started = false;
        onresult: RecogHandlers['onresult'] = null;
        onerror: RecogHandlers['onerror'] = null;
        onend: RecogHandlers['onend'] = null;
        constructor() {
          created.push(
            this as unknown as RecogHandlers & {
              lang: string;
              continuous: boolean;
              interimResults: boolean;
              started: boolean;
            },
          );
        }
        start(): void {
          this.started = true;
        }
        stop(): void {
          this.onend?.();
        }
      }
      (window as unknown as { SpeechRecognition: typeof MockRecognition }).SpeechRecognition = MockRecognition;
      (window as unknown as { __mockRecognitions: typeof created }).__mockRecognitions = created;
    });
  };

  test('mic button is rendered when speech recognition is supported', async ({ authenticatedPage }) => {
    await installMockRecognition(authenticatedPage);
    await authenticatedPage.reload();

    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    const mic = authenticatedPage.locator('[data-testid="chat-mic"]');
    await expect(mic).toBeVisible();
  });

  test('clicking the mic populates the input with the spoken transcript', async ({ authenticatedPage }) => {
    await installMockRecognition(authenticatedPage);
    await authenticatedPage.reload();

    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    const mic = authenticatedPage.locator('[data-testid="chat-mic"]');
    await mic.click();

    // Drive the recognition session — fire onresult with the transcript,
    // then onend so isListening flips back to false.
    await authenticatedPage.evaluate(() => {
      const created = (
        window as unknown as {
          __mockRecognitions: Array<{
            onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
            onend: (() => void) | null;
          }>;
        }
      ).__mockRecognitions;
      const recognition = created[created.length - 1];
      recognition.onresult?.({ results: [[{ transcript: 'add milk to my shopping list' }]] });
      recognition.onend?.();
    });

    await expect(chat.input).toHaveValue('add milk to my shopping list');
  });

  test('mic button shows the listening visual state while recognition is active', async ({ authenticatedPage }) => {
    await installMockRecognition(authenticatedPage);
    await authenticatedPage.reload();

    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    const mic = authenticatedPage.locator('[data-testid="chat-mic"]');
    await mic.click();

    await expect(mic).toHaveClass(/listening/);
    await expect(mic).toHaveAttribute('aria-pressed', 'true');
  });

  test('shows the no-speech hint when recognition ends without a transcript (TC-360-05)', async ({ authenticatedPage }) => {
    await installMockRecognition(authenticatedPage);
    await authenticatedPage.reload();

    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    await authenticatedPage.locator('[data-testid="chat-mic"]').click();

    // Browser's silence-timeout path: onerror with 'no-speech' then onend.
    await authenticatedPage.evaluate(() => {
      const created = (
        window as unknown as {
          __mockRecognitions: Array<{
            onerror: ((event: { error: string }) => void) | null;
            onend: (() => void) | null;
          }>;
        }
      ).__mockRecognitions;
      const recognition = created[created.length - 1];
      recognition.onerror?.({ error: 'no-speech' });
      recognition.onend?.();
    });

    const errorBanner = authenticatedPage.locator('.chat-error[role="alert"]');
    await expect(errorBanner).toBeVisible();
  });
});
