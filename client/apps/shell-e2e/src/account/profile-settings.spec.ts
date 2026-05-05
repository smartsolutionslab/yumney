import { test, expect } from '../fixtures/auth.fixture';
import { AccountPage } from '../pages/account.page';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Profile Settings (US-302)', () => {
  test('should display the profile settings page', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.title).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should show settings sections', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.title).toBeVisible({ timeout: TIMEOUTS.default });
    // US-100 redesign: profile, language/units, theme, household/dietary, voice, notifications.
    await expect(account.settingsSections).toHaveCount(6);
  });

  test('should display servings input with default value', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.servingsInput).toBeVisible({ timeout: TIMEOUTS.default });

    const value = await account.servingsInput.inputValue();
    expect(Number(value)).toBeGreaterThanOrEqual(1);
    expect(Number(value)).toBeLessThanOrEqual(12);
  });

  test('should display dietary type select', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.dietaryTypeSelect).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should display restriction checkboxes', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.checkboxLabels.first()).toBeVisible({ timeout: TIMEOUTS.default });

    const count = await account.checkboxLabels.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('should auto-save changes and show saved indicator', async ({ authenticatedPage }) => {
    // US-100 replaced the explicit Save button with debounced auto-save.
    // Changing any field triggers a save; the saved indicator appears once
    // the round-trip completes.
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.servingsInput).toBeVisible({ timeout: TIMEOUTS.default });

    const current = await account.servingsInput.inputValue();
    const next = Number(current) === 4 ? 3 : 4;
    await account.servingsInput.fill(String(next));
    await account.servingsInput.blur();

    await expect(account.savedIndicator).toBeVisible({ timeout: TIMEOUTS.default });
  });
});
