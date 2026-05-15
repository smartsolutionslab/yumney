import { test, expect } from '../fixtures/auth.fixture';
import { ChatPage } from '../pages/chat.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Recipe Chat (US-230, US-303, US-306)', () => {
  test('should open chat panel when FAB is clicked', async ({ authenticatedPage }) => {
    const chat = new ChatPage(authenticatedPage);
    await expect(chat.fab).toBeVisible({ timeout: TIMEOUTS.default });

    await chat.open();

    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });
  });

  // FAB-as-close removed in e0f43316: the FAB sat fixed bottom-right with
  // z-index above the modal, so once the chat panel opened the FAB's X icon
  // overlapped the panel's send button (also bottom-right) and made send
  // unreachable. The chat header's own close button is now the canonical
  // close affordance — covered by 'should close chat panel when close
  // button is clicked' below.

  test('should close chat panel when close button is clicked', async ({ authenticatedPage }) => {
    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    await chat.close();

    await expect(chat.panel).not.toBeVisible();
  });

  test('should close chat panel when backdrop is tapped on mobile', async ({ authenticatedPage }) => {
    await authenticatedPage.setViewportSize({ width: 375, height: 812 });
    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    await expect(chat.backdrop).toBeVisible();
    await chat.backdrop.click({ position: { x: 10, y: 10 } });

    await expect(chat.panel).not.toBeVisible();
  });

  test('should show welcome message with examples when panel first opens', async ({ authenticatedPage }) => {
    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    await expect(chat.welcome).toBeVisible();
    await expect(chat.examples).toHaveCount(3);
  });

  test('should show context-aware placeholder', async ({ authenticatedPage }) => {
    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    const placeholder = await chat.input.getAttribute('placeholder');
    expect(placeholder?.length).toBeGreaterThan(0);
  });

  test('should send a message and receive a response', async ({ authenticatedPage }) => {
    const chat = new ChatPage(authenticatedPage);
    await chat.open();
    await expect(chat.panel).toBeVisible({ timeout: TIMEOUTS.short });

    await chat.sendMessage('What can I cook tonight?');

    await expect(chat.userMessage()).toBeVisible();
    await expect(chat.assistantMessage()).toBeVisible({ timeout: TIMEOUTS.veryLong });
  });

  test('should show FAB with aria-expanded attribute', async ({ authenticatedPage }) => {
    const chat = new ChatPage(authenticatedPage);
    await expect(chat.fab).toBeVisible({ timeout: TIMEOUTS.default });
    await expect(chat.fab).toHaveAttribute('aria-expanded', 'false');

    await chat.open();
    await expect(chat.fab).toHaveAttribute('aria-expanded', 'true');
  });
});
