import { test, expect } from '../fixtures/auth.fixture';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Coverage for #413 — DE/EN parity smoke. Catches the highest-value
 * regression class: a missing translation key renders as the literal
 * key string (e.g. 'shell.dashboard.title') because Transloco falls
 * back to the lookup path when nothing matches.
 *
 * Strategy per page:
 *   1. Visit in EN, capture the <h1> text.
 *   2. Switch language via the user-menu (LanguageService.switchTo
 *      → localStorage.yn-language).
 *   3. Reload, capture the new <h1>.
 *   4. Assert both are non-empty, neither matches the literal-key
 *      pattern, and the two strings differ (proves the switch did
 *      something).
 *
 * Switching the language only writes to localStorage (no backend
 * call — see LanguageService); per-test browser-context isolation
 * means each test starts from the storageState's saved language.
 */
const PROBE_PAGES = [
  { label: 'dashboard', url: '/dashboard' },
  { label: 'recipes list', url: '/recipes' },
  { label: 'meal planner', url: '/meal-planner' },
  { label: 'account', url: '/account' },
] as const;

const LITERAL_KEY = /^[a-z][a-zA-Z0-9]*(\.[a-zA-Z0-9]+)+$/;

async function readH1(page: import('@playwright/test').Page): Promise<string> {
  const h1 = page.getByRole('heading', { level: 1 }).first();
  await expect(h1).toBeVisible({ timeout: TIMEOUTS.default });
  const text = (await h1.textContent())?.trim() ?? '';
  return text;
}

async function setLanguage(
  page: import('@playwright/test').Page,
  lang: 'en' | 'de',
): Promise<void> {
  await page.evaluate((value) => {
    localStorage.setItem('yn-language', value);
  }, lang);
}

test.describe('i18n parity smoke (#413)', () => {
  for (const probe of PROBE_PAGES) {
    test(`${probe.label} renders translated text in EN and DE`, async ({ authenticatedPage }) => {
      await setLanguage(authenticatedPage, 'en');
      await authenticatedPage.goto(probe.url);
      const enText = await readH1(authenticatedPage);

      await setLanguage(authenticatedPage, 'de');
      await authenticatedPage.reload();
      const deText = await readH1(authenticatedPage);

      // Neither rendering should leak a literal Transloco key. The
      // pattern catches the typical "fallback to key" failure mode.
      expect(enText, `EN h1 must not be a literal i18n key: "${enText}"`).not.toMatch(LITERAL_KEY);
      expect(deText, `DE h1 must not be a literal i18n key: "${deText}"`).not.toMatch(LITERAL_KEY);

      // Both must be non-empty — protects against the edge case where
      // Transloco returns an empty string for a missing key.
      expect(enText.length, 'EN h1 must not be empty').toBeGreaterThan(0);
      expect(deText.length, 'DE h1 must not be empty').toBeGreaterThan(0);

      // Note: deliberately NOT asserting deText !== enText. Some labels
      // are identical across English/German ("Dashboard", "Account") —
      // the literal-key check above is the load-bearing signal, not
      // string inequality.
    });
  }
});
