import { type Page, type Locator } from '@playwright/test';

/**
 * Profile-settings page at /account. After the US-100 redesign the page
 * renders six collapsible yn-settings-card sections (profile, language &
 * units, theme, household & dietary, voice, notifications), each marked
 * with [data-testid="profile-settings-section"]. Saving is debounced
 * auto-save — there is no explicit Save button. Loading / error / retry
 * are owned by the shared yn-async-state component (class-only).
 */
export class AccountPage {
  readonly heading: Locator;
  readonly title: Locator;
  readonly settingsSections: Locator;
  readonly servingsInput: Locator;
  readonly dietaryTypeSelect: Locator;
  readonly cookingEffortSelect: Locator;
  readonly checkboxLabels: Locator;
  readonly savedIndicator: Locator;
  readonly loading: Locator;
  readonly error: Locator;
  readonly retryButton: Locator;

  constructor(private page: Page) {
    this.heading = page.getByRole('heading', { level: 1 });
    this.title = page.locator('[data-testid="profile-settings-title"]');
    this.settingsSections = page.locator('[data-testid="profile-settings-section"]');
    this.servingsInput = page.locator('#servings');
    this.dietaryTypeSelect = page.locator('#dietaryType');
    this.cookingEffortSelect = page.locator('#cookingEffort');
    this.checkboxLabels = page.locator('.checkbox-label');
    this.savedIndicator = page.locator('[data-testid="profile-saved-indicator"]');
    this.loading = page.locator('.loading');
    this.error = page.locator('.error');
    this.retryButton = page.locator('.retry-btn');
  }

  async goto(): Promise<void> {
    await this.page.goto('/account');
  }
}
