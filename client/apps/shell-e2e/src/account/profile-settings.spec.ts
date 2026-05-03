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
    await expect(account.settingsSections).toHaveCount(3); // Household, Dietary, Weekly Goals
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

  test('should have a save button', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.saveButton).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should save profile and show success indicator', async ({ authenticatedPage }) => {
    const account = new AccountPage(authenticatedPage);
    await account.goto();

    await expect(account.saveButton).toBeVisible({ timeout: TIMEOUTS.default });

    await account.saveButton.click();

    await expect(account.savedIndicator).toBeVisible({ timeout: TIMEOUTS.default });
  });
});
