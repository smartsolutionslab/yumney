import AxeBuilder from '@axe-core/playwright';
import { test, expect } from '../fixtures/auth.fixture';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Coverage for #411 — accessibility smoke. Runs axe-core against the
 * primary authenticated views and fails the test on serious/critical
 * violations only. Moderate / minor violations are not gated; a future
 * follow-up can tighten the bar once the suite is consistently green.
 *
 * Each spec asserts on its own page in isolation so a single page's
 * regression doesn't mask others.
 *
 * If a page surfaces a pre-existing serious/critical issue at the time
 * this lands, the right move is to file a follow-up frontend bug and
 * exclude the specific rule via .disableRules([...]) with an issue
 * reference rather than lower the bar globally.
 */
const SERIOUS_OR_CRITICAL = ['serious', 'critical'] as const;

// Pre-existing serious violations on develop, tracked in #443. Disabled
// here so this smoke can land green and surface NEW regressions on top
// of the known-bad baseline. Remove once #443 ships.
const DISABLED_RULES_PENDING_443 = ['color-contrast', 'aria-command-name'];

async function runAxe(page: import('@playwright/test').Page): Promise<void> {
  const results = await new AxeBuilder({ page })
    // Limit to WCAG 2.1 AA — matches CLAUDE.md's stated target. Other
    // best-practice axe rules can flake on Angular's component classes.
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .disableRules(DISABLED_RULES_PENDING_443)
    .analyze();

  const blocking = results.violations.filter((v) =>
    SERIOUS_OR_CRITICAL.includes(v.impact as (typeof SERIOUS_OR_CRITICAL)[number]),
  );

  if (blocking.length > 0) {
    const summary = blocking.map((v) => ({
      id: v.id,
      impact: v.impact,
      help: v.help,
      nodes: v.nodes.length,
      sample: v.nodes[0]?.target,
    }));
    // Surface the violations in the test failure context.
    // eslint-disable-next-line no-console
    console.log('axe violations (serious/critical):', JSON.stringify(summary, null, 2));
  }

  expect(blocking, 'serious / critical axe violations on this page').toEqual([]);
}

test.describe('A11y smoke (#411)', () => {
  test('dashboard', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/dashboard');
    await expect(authenticatedPage.getByRole('heading', { level: 1 })).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await runAxe(authenticatedPage);
  });

  test('recipes list', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/recipes');
    await expect(authenticatedPage.getByRole('heading', { level: 1 })).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await runAxe(authenticatedPage);
  });

  test('shopping', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/shopping');
    await expect(authenticatedPage.locator('app-root, yn-root, main').first()).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await runAxe(authenticatedPage);
  });

  test('meal planner', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/meal-planner');
    await expect(authenticatedPage.getByRole('heading', { level: 1 })).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await runAxe(authenticatedPage);
  });

  test('account profile', async ({ authenticatedPage }) => {
    await authenticatedPage.goto('/account/profile');
    await expect(authenticatedPage.getByRole('heading', { level: 1 })).toBeVisible({
      timeout: TIMEOUTS.default,
    });
    await runAxe(authenticatedPage);
  });
});
