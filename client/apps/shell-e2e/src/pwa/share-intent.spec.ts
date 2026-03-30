import { test, expect } from '@playwright/test';

test.describe('PWA Share Intent (US-124)', () => {
  test('should declare share_target in manifest', async ({ page }) => {
    const response = await page.goto('/manifest.webmanifest');
    const manifest = await response?.json();

    expect(manifest.share_target).toBeDefined();
    expect(manifest.share_target.action).toBe('/dashboard');
    expect(manifest.share_target.method).toBe('GET');
    expect(manifest.share_target.params.url).toBe('url');
    expect(manifest.share_target.params.text).toBe('text');
  });
});
