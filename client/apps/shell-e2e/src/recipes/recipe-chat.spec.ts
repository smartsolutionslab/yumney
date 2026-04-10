import { test, expect } from '../fixtures/auth.fixture';

test.describe('Recipe Chat (US-230, US-303)', () => {
  test('should open chat panel when FAB is clicked', async ({ authenticatedPage }) => {
    const fab = authenticatedPage.locator('.command-fab');
    await expect(fab).toBeVisible({ timeout: 10_000 });

    await fab.click();

    const chatPanel = authenticatedPage.locator('.chat-panel');
    await expect(chatPanel).toBeVisible({ timeout: 5_000 });
  });

  test('should close chat panel when FAB is clicked again', async ({ authenticatedPage }) => {
    const fab = authenticatedPage.locator('.command-fab');
    await fab.click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    await fab.click();

    await expect(authenticatedPage.locator('.chat-panel')).not.toBeVisible();
  });

  test('should close chat panel when close button is clicked', async ({ authenticatedPage }) => {
    await authenticatedPage.locator('.command-fab').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    await authenticatedPage.locator('.chat-close').click();

    await expect(authenticatedPage.locator('.chat-panel')).not.toBeVisible();
  });

  test('should close chat panel when backdrop is tapped on mobile', async ({
    authenticatedPage,
  }) => {
    await authenticatedPage.setViewportSize({ width: 375, height: 812 });
    await authenticatedPage.locator('.command-fab').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    const backdrop = authenticatedPage.locator('.chat-backdrop');
    await expect(backdrop).toBeVisible();
    await backdrop.click({ position: { x: 10, y: 10 } });

    await expect(authenticatedPage.locator('.chat-panel')).not.toBeVisible();
  });

  test('should show welcome message with examples when panel first opens', async ({
    authenticatedPage,
  }) => {
    await authenticatedPage.locator('.command-fab').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    await expect(authenticatedPage.locator('.chat-welcome')).toBeVisible();
    await expect(authenticatedPage.locator('.chat-examples li')).toHaveCount(3);
  });

  test('should show context-aware placeholder on dashboard', async ({ authenticatedPage }) => {
    await authenticatedPage.locator('.command-fab').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    const textarea = authenticatedPage.locator('.chat-input textarea');
    const placeholder = await textarea.getAttribute('placeholder');
    expect(placeholder).toBeTruthy();
    expect(placeholder!.length).toBeGreaterThan(0);
  });

  test('should send a message and receive a response', async ({ authenticatedPage }) => {
    await authenticatedPage.locator('.command-fab').click();
    await expect(authenticatedPage.locator('.chat-panel')).toBeVisible({ timeout: 5_000 });

    const textarea = authenticatedPage.locator('.chat-input textarea');
    await textarea.fill('What can I cook tonight?');
    await authenticatedPage.locator('.chat-send').click();

    await expect(authenticatedPage.locator('.chat-message.role-user .chat-bubble')).toBeVisible();

    await expect(
      authenticatedPage.locator('.chat-message.role-assistant .chat-bubble'),
    ).toBeVisible({ timeout: 30_000 });
  });

  test('should show FAB with aria-expanded attribute', async ({ authenticatedPage }) => {
    const fab = authenticatedPage.locator('.command-fab');
    await expect(fab).toBeVisible({ timeout: 10_000 });
    await expect(fab).toHaveAttribute('aria-expanded', 'false');

    await fab.click();
    await expect(fab).toHaveAttribute('aria-expanded', 'true');
  });
});
