import { test, expect } from '@playwright/test';
import { isServiceWorkerRegistered } from '../helpers/pwa.helper';

test.describe('PWA Installation', () => {
  test('should serve a valid web manifest', async ({ page }) => {
    const response = await page.goto('/manifest.webmanifest');
    expect(response?.status()).toBe(200);

    const manifest = await response?.json();
    expect(manifest.name).toBe('Yumney');
    expect(manifest.short_name).toBe('Yumney');
    expect(manifest.display).toBe('standalone');
    expect(manifest.start_url).toBe('/');
    expect(manifest.icons.length).toBeGreaterThanOrEqual(2);
  });

  test('should link manifest in index.html', async ({ page }) => {
    await page.route('**/realms/**', (route) => route.abort());
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    const manifestLink = page.locator('link[rel="manifest"]');
    await expect(manifestLink).toHaveAttribute('href', 'manifest.webmanifest');
  });

  test('should have theme-color meta tag', async ({ page }) => {
    await page.route('**/realms/**', (route) => route.abort());
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    const themeColor = page.locator('meta[name="theme-color"]');
    await expect(themeColor).toHaveAttribute('content', '#f97316');
  });

  test('should serve PWA icons', async ({ page }) => {
    const icon192 = await page.goto('/assets/icons/icon-192x192.png');
    expect(icon192?.status()).toBe(200);
    expect(icon192?.headers()['content-type']).toContain('image/png');

    const icon512 = await page.goto('/assets/icons/icon-512x512.png');
    expect(icon512?.status()).toBe(200);
  });

  // Angular's provideServiceWorker is configured with `enabled: !isDevMode()`.
  // The E2E suite runs against `nx serve shell` (dev mode), so no SW registers
  // and this test can never pass against the dev pipeline. Either run against
  // a built-and-served bundle or keep this skipped — the manifest/icons tests
  // above already cover the static-asset side of PWA-readiness.
  test.skip('should register a service worker', async ({ page }) => {
    await page.route('**/realms/**', (route) => route.abort());
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    const registered = await isServiceWorkerRegistered(page);
    expect(registered).toBe(true);
  });
});
