import { test, expect } from '../fixtures/auth.fixture';
import { SELECTORS } from '../helpers/selectors';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Profile Settings (US-302)', () => {
  test('should display the profile settings page', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    await expect(authenticatedPage.locator(SELECTORS.profileSettings.title)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should show settings sections', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    await expect(authenticatedPage.locator(SELECTORS.profileSettings.title)).toBeVisible({
      timeout: TIMEOUTS.default,
    });

    const sections = authenticatedPage.locator(SELECTORS.profileSettings.settingsSection);
    await expect(sections).toHaveCount(3); // Household, Dietary, Weekly Goals
  });

  test('should display servings input with default value', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    const input = authenticatedPage.locator(SELECTORS.profileSettings.servingsInput);
    await expect(input).toBeVisible({ timeout: TIMEOUTS.default });

    const value = await input.inputValue();
    expect(Number(value)).toBeGreaterThanOrEqual(1);
    expect(Number(value)).toBeLessThanOrEqual(12);
  });

  test('should display dietary type select', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    await expect(
      authenticatedPage.locator(SELECTORS.profileSettings.dietaryTypeSelect),
    ).toBeVisible({ timeout: TIMEOUTS.default });
  });

  test('should display restriction checkboxes', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    await expect(
      authenticatedPage.locator(SELECTORS.profileSettings.checkboxLabel).first(),
    ).toBeVisible({ timeout: TIMEOUTS.default });

    const checkboxes = authenticatedPage.locator(SELECTORS.profileSettings.checkboxLabel);
    const count = await checkboxes.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });

  test('should have a save button', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    await expect(authenticatedPage.locator(SELECTORS.profileSettings.saveBtn)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });

  test('should save profile and show success indicator', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account');

    const saveBtn = authenticatedPage.locator(SELECTORS.profileSettings.saveBtn);
    await expect(saveBtn).toBeVisible({ timeout: TIMEOUTS.default });

    await saveBtn.click();

    await expect(authenticatedPage.locator(SELECTORS.profileSettings.savedIndicator)).toBeVisible({
      timeout: TIMEOUTS.default,
    });
  });
});
