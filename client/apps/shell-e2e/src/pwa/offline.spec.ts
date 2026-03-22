import { test, expect, Page } from '@playwright/test';

async function setupKeycloakMock(page: Page) {
  await page.route(
    '**/realms/yumney/.well-known/openid-configuration',
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          issuer: 'http://localhost:8080/realms/yumney',
          authorization_endpoint:
            'http://localhost:8080/realms/yumney/protocol/openid-connect/auth',
          token_endpoint:
            'http://localhost:8080/realms/yumney/protocol/openid-connect/token',
          end_session_endpoint:
            'http://localhost:8080/realms/yumney/protocol/openid-connect/logout',
          jwks_uri:
            'http://localhost:8080/realms/yumney/protocol/openid-connect/certs',
          userinfo_endpoint:
            'http://localhost:8080/realms/yumney/protocol/openid-connect/userinfo',
          response_types_supported: ['code'],
          subject_types_supported: ['public'],
          id_token_signing_alg_values_supported: ['RS256'],
        }),
      }),
  );
  await page.route(
    '**/realms/yumney/protocol/openid-connect/auth*',
    (route) => route.fulfill({ status: 200, body: '' }),
  );
  await page.route(
    '**/realms/yumney/protocol/openid-connect/certs',
    (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ keys: [] }),
      }),
  );
}

async function waitForServiceWorker(page: Page) {
  await page.evaluate(async () => {
    if (!('serviceWorker' in navigator)) {
      return;
    }
    const deadline = Date.now() + 40000;
    while (Date.now() < deadline) {
      const registrations = await navigator.serviceWorker.getRegistrations();
      const active = registrations.find(
        (r) => r.active?.state === 'activated',
      );
      if (active) {
        return;
      }
      await new Promise((r) => setTimeout(r, 500));
    }
  });
}

test.describe('Offline Caching', () => {
  test.beforeEach(async ({ page }) => {
    await setupKeycloakMock(page);
    await page.goto('/', { waitUntil: 'networkidle' });
    await waitForServiceWorker(page);
    await page.reload({ waitUntil: 'networkidle' });
  });

  test('should serve cached app shell when offline', async ({
    page,
    context,
  }) => {
    await page.waitForLoadState('networkidle');

    await context.setOffline(true);
    await page.reload({ waitUntil: 'domcontentloaded' });

    const root = page.locator('yn-root');
    await expect(root).toBeAttached({ timeout: 10000 });
  });

  test('should serve cached i18n translations when offline', async ({
    page,
    context,
  }) => {
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
