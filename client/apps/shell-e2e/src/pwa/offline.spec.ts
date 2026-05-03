import { test, expect } from '@playwright/test';
import { AppShellPage } from '../pages/app-shell.page';
import {
  setupKeycloakMock,
  waitForServiceWorker,
  waitForServiceWorkerControl,
} from '../helpers/pwa.helper';
import { TIMEOUTS } from '../helpers/timeouts';

test.describe('Offline Caching', () => {
  test.beforeEach(async ({ page }) => {
    await setupKeycloakMock(page);
    // domcontentloaded is enough — the real readiness signals are the SW
    // helpers below. networkidle is a known anti-pattern under our Aspire
    // stack: OTLP/log streams keep the network busy and it can never fire.
    await page.goto('/', { waitUntil: 'domcontentloaded' });
    await waitForServiceWorker(page);
    // Reload so the now-active SW claims the page as a client.
    await page.reload({ waitUntil: 'domcontentloaded' });
    // Wait until the SW actually controls fetches AND prime the cache for
    // the URLs the offline tests care about. Without this the tests race
    // the lazy NGSW cache population and see nothing.
    await waitForServiceWorkerControl(page);
  });

  test('should serve cached app shell when offline', async ({ page, context }) => {
    // beforeEach has already awaited SW control and primed the cache, so we
    // can go offline immediately — no extra wait needed.
    await context.setOffline(true);
    await page.reload({ waitUntil: 'domcontentloaded' });

    const shell = new AppShellPage(page);
    await expect(shell.root).toBeAttached({ timeout: TIMEOUTS.default });
  });

  test('should serve cached i18n translations when offline', async ({ page, context }) => {
    // Read the body so the SW fetch handler completes the cache write before
    // we go offline. Awaiting only the Response headers (or relying on
    // networkidle) leaves a race where the cache may not be populated yet.
    await page.evaluate(async () => {
      const res = await fetch('/assets/i18n/en.json');
      await res.text();
    });

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
