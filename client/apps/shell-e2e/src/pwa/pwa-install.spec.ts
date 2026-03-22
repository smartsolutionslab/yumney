import { test, expect } from '@playwright/test';

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

  test('should register a service worker', async ({ page }) => {
    await page.route('**/realms/**', (route) => route.abort());
    await page.goto('/', { waitUntil: 'domcontentloaded' });

    const swRegistered = await page.evaluate(async () => {
      if (!('serviceWorker' in navigator)) {
        return false;
      }
      const deadline = Date.now() + 35000;
      while (Date.now() < deadline) {
        const registrations = await navigator.serviceWorker.getRegistrations();
        if (registrations.length > 0) {
          return true;
        }
        await new Promise((r) => setTimeout(r, 500));
      }
      return false;
    });
    expect(swRegistered).toBe(true);
  });
});
