import { test, expect } from '../fixtures/auth.fixture';

test.describe('Recipe Chat (US-230)', () => {
  test('should open chat panel when toggle button is clicked', async ({ authenticatedPage }) => {
    const chatToggle = authenticatedPage.locator('.chat-toggle');
    await expect(chatToggle).toBeVisible({ timeout: 10_000 });

    await chatToggle.click();

    const chatPanel = authenticatedPage.locator('.chat-panel');
    await expect(chatPanel).toBeVisible({ timeout: 5_000 });
  });

  test('should close chat panel when close button is clicked', async ({ authenticatedPage }) => {
    await authenticatedPage.locator('.chat-toggle').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    await authenticatedPage.locator('.chat-close').click();

    await expect(authenticatedPage.locator('.chat-panel')).not.toBeVisible();
  });

  test('should close chat panel when backdrop is tapped on mobile', async ({
    authenticatedPage,
  }) => {
    await authenticatedPage.setViewportSize({ width: 375, height: 812 });
    await authenticatedPage.locator('.chat-toggle').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    const backdrop = authenticatedPage.locator('.chat-backdrop');
    await expect(backdrop).toBeVisible();
    await backdrop.click({ position: { x: 10, y: 10 } });

    await expect(authenticatedPage.locator('.chat-panel')).not.toBeVisible();
  });

  test('should show welcome message with examples when panel first opens', async ({
    authenticatedPage,
  }) => {
    await authenticatedPage.locator('.chat-toggle').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    await expect(authenticatedPage.locator('.chat-welcome')).toBeVisible();
    await expect(authenticatedPage.locator('.chat-examples li')).toHaveCount(3);
  });

  test('should send a message and receive a response', async ({ authenticatedPage }) => {
    await authenticatedPage.locator('.chat-toggle').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    const textarea = authenticatedPage.locator('.chat-input textarea');
    await textarea.fill('What can I cook tonight?');
    await authenticatedPage.locator('.chat-send').click();

    // User message appears
    await expect(authenticatedPage.locator('.chat-message.role-user .chat-bubble')).toBeVisible();

    // Wait for assistant response (may take a few seconds for LLM)
    await expect(
      authenticatedPage.locator('.chat-message.role-assistant .chat-bubble'),
    ).toBeVisible({ timeout: 30_000 });
  });
});
