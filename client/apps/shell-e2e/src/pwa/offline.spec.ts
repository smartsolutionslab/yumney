import { test, expect } from '@playwright/test';
import {
  setupKeycloakMock,
  waitForServiceWorker,
  waitForServiceWorkerControl,
} from '../helpers/pwa.helper';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Offline Caching', () => {
  test.beforeEach(async ({ page }) => {
    await setupKeycloakMock(page);
    await page.goto('/', { waitUntil: 'networkidle' });
    await waitForServiceWorker(page);
    // Reload so the now-active SW claims the page as a client.
    await page.reload({ waitUntil: 'networkidle' });
    // Wait until the SW actually controls fetches AND prime the cache for
    // the URLs the offline tests care about. Without this the tests race
    // the lazy NGSW cache population and see nothing.
    await waitForServiceWorkerControl(page);
  });

  test('should serve cached app shell when offline', async ({ page, context }) => {
    await page.waitForLoadState('networkidle');

    await context.setOffline(true);
    await page.reload({ waitUntil: 'domcontentloaded' });

    const root = page.locator('yn-root');
    await expect(root).toBeAttached({ timeout: TIMEOUTS.default });
  });

  test('should serve cached i18n translations when offline', async ({ page, context }) => {
    await page.evaluate(() => fetch('/assets/i18n/en.json'));
    await page.waitForLoadState('networkidle');

    await context.setOffline(true);

    const response = await page.evaluate(async () => {
      try {
        const res = await fetch('/assets/i18n/en.json');
        return { ok: res.ok, status: res.status };
      } catch {
        return { ok: false, status: 0 };
      }
    });

    expect(response.ok).toBe(true);
    expect(response.status).toBe(200);

    await context.setOffline(false);
  });
});
