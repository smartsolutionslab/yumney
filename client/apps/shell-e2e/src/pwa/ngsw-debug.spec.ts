import { test, expect } from '@playwright/test';
import { setupKeycloakMock } from '../helpers/pwa.helper';

// Temporary investigation spec for #423: dumps everything Chromium sees
// while NGSW is supposed to install + prefetch. The earlier CI diagnostic
// proved the static serve chain is healthy (all assets return 200 with
// correct sizes), so the problem must be browser-side. This spec captures
// page console messages, page errors, registered SWs, controller state,
// per-cache contents, and HTTP response statuses observed by the browser
// — anything that explains why NGSW's caches stay empty after activation.
//
// Always fails intentionally so the Playwright trace + screenshot artifact
// is preserved for inspection.
test.describe('NGSW debug (#423)', () => {
  test('capture SW activity and cache state', async ({ page }) => {
    const events: string[] = [];
    page.on('console', (m) => events.push(`[console.${m.type()}] ${m.text()}`));
    page.on('pageerror', (e) => events.push(`[pageerror] ${e.message}\n${e.stack ?? ''}`));
    page.on('requestfailed', (req) =>
      events.push(`[reqfail] ${req.method()} ${req.url()} -> ${req.failure()?.errorText}`),
    );
    page.on('response', (r) => {
      const url = r.url();
      if (url.includes('ngsw') || url.includes('chunk-') || url.endsWith('.json')) {
        events.push(`[resp] ${r.status()} ${url}`);
      }
    });

    await setupKeycloakMock(page);
    await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 60_000 });

    // Give NGSW a generous window to install + prefetch.
    await page.waitForTimeout(15_000);
    // Force a reload so the freshly-activated SW takes control.
    await page.reload({ waitUntil: 'domcontentloaded', timeout: 60_000 });
    await page.waitForTimeout(15_000);

    const swInfo = await page.evaluate(async () => {
      if (!('serviceWorker' in navigator)) return { supported: false };
      const regs = await navigator.serviceWorker.getRegistrations();
      return {
        supported: true,
        registrationCount: regs.length,
        controller: navigator.serviceWorker.controller?.scriptURL ?? null,
        registrations: regs.map((r) => ({
          scope: r.scope,
          active: r.active ? { state: r.active.state, scriptURL: r.active.scriptURL } : null,
          installing: r.installing ? { state: r.installing.state } : null,
          waiting: r.waiting ? { state: r.waiting.state } : null,
        })),
      };
    });

    const cacheInfo = await page.evaluate(async () => {
      const names = await caches.keys();
      const detail: Array<{ name: string; count: number; sample: string[] }> = [];
      for (const name of names) {
        const cache = await caches.open(name);
        const keys = await cache.keys();
        detail.push({
          name,
          count: keys.length,
          sample: keys.slice(0, 5).map((k) => k.url),
        });
      }
      return detail;
    });

    // Try to ping the NGSW Driver directly via SW message.
    const driverState = await page.evaluate(async () => {
      const ctrl = navigator.serviceWorker.controller;
      if (!ctrl) return { reachable: false };
      try {
        return await new Promise<unknown>((resolve, reject) => {
          const timer = setTimeout(() => reject(new Error('sw message timeout')), 5_000);
          const channel = new MessageChannel();
          channel.port1.onmessage = (e) => {
            clearTimeout(timer);
            resolve(e.data);
          };
          ctrl.postMessage({ action: 'CHECK_FOR_UPDATES' }, [channel.port2]);
        });
      } catch (err) {
        return { reachable: false, error: String(err) };
      }
    });

    // Surface to CI logs.
    // eslint-disable-next-line no-console
    console.log('=== NGSW DEBUG (#423) ===');
    // eslint-disable-next-line no-console
    console.log('SW INFO:', JSON.stringify(swInfo, null, 2));
    // eslint-disable-next-line no-console
    console.log('CACHE INFO:', JSON.stringify(cacheInfo, null, 2));
    // eslint-disable-next-line no-console
    console.log('DRIVER PING:', JSON.stringify(driverState, null, 2));
    // eslint-disable-next-line no-console
    console.log('--- EVENTS (last 80) ---\n' + events.slice(-80).join('\n'));

    // Intentional fail so Playwright preserves trace + video artifacts.
    expect(events.find((e) => e.startsWith('[NGSW-DEBUG-EXPECTED-FAIL]'))).toBeTruthy();
  });
});
