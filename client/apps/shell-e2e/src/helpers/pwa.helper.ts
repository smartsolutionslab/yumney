import { type Page } from '@playwright/test';

/**
 * Mock Keycloak OIDC endpoints so PWA tests can load without a real auth server.
 */
export async function setupKeycloakMock(page: Page): Promise<void> {
  await page.route('**/realms/yumney/.well-known/openid-configuration', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        issuer: 'http://localhost:8080/realms/yumney',
        authorization_endpoint: 'http://localhost:8080/realms/yumney/protocol/openid-connect/auth',
        token_endpoint: 'http://localhost:8080/realms/yumney/protocol/openid-connect/token',
        end_session_endpoint: 'http://localhost:8080/realms/yumney/protocol/openid-connect/logout',
        jwks_uri: 'http://localhost:8080/realms/yumney/protocol/openid-connect/certs',
        userinfo_endpoint: 'http://localhost:8080/realms/yumney/protocol/openid-connect/userinfo',
        response_types_supported: ['code'],
        subject_types_supported: ['public'],
        id_token_signing_alg_values_supported: ['RS256'],
      }),
    }),
  );
  await page.route('**/realms/yumney/protocol/openid-connect/auth*', (route) =>
    route.fulfill({ status: 200, body: '' }),
  );
  await page.route('**/realms/yumney/protocol/openid-connect/certs', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ keys: [] }),
    }),
  );
}

/**
 * Wait for the service worker to become activated AND control the page.
 * For Angular's NGSW the controller is only set on the next navigation
 * after registration, so callers should reload after this resolves if
 * they need the SW to actually serve subsequent fetches.
 */
export async function waitForServiceWorker(page: Page, timeout = 40_000): Promise<void> {
  await page.evaluate(async (ms) => {
    if (!('serviceWorker' in navigator)) return;
    const deadline = Date.now() + ms;
    while (Date.now() < deadline) {
      const registrations = await navigator.serviceWorker.getRegistrations();
      const active = registrations.find((r) => r.active?.state === 'activated');
      if (active) return;
      await new Promise((r) => setTimeout(r, 500));
    }
  }, timeout);
}

/**
 * Wait until the page is being controlled by an active service worker.
 * Required after a reload following SW activation: NGSW sets
 * navigator.serviceWorker.controller asynchronously and the offline
 * tests need a controlled fetch path. Also primes a few well-known
 * URLs so the SW has them cached before the test goes offline.
 */
export async function waitForServiceWorkerControl(page: Page, timeout = 40_000): Promise<void> {
  await page.evaluate(async (ms) => {
    if (!('serviceWorker' in navigator)) return;
    const deadline = Date.now() + ms;
    while (Date.now() < deadline && !navigator.serviceWorker.controller) {
      await new Promise((r) => setTimeout(r, 100));
    }
    // Prime the cache for the URLs the offline tests care about. The Angular
    // SW asset/data groups are populated lazily on first fetch through the
    // SW; without this, an immediate setOffline + reload races the cache
    // and serves nothing.
    const warmupUrls = ['/', '/index.html', '/assets/i18n/en.json'];
    await Promise.all(
      warmupUrls.map((url) =>
        fetch(url, { cache: 'no-store' }).catch(() => undefined),
      ),
    );
  }, timeout);
}

/**
 * Check if any service worker is registered.
 */
export async function isServiceWorkerRegistered(page: Page, timeout = 35_000): Promise<boolean> {
  return page.evaluate(async (ms) => {
    if (!('serviceWorker' in navigator)) return false;
    const deadline = Date.now() + ms;
    while (Date.now() < deadline) {
      const registrations = await navigator.serviceWorker.getRegistrations();
      if (registrations.length > 0) return true;
      await new Promise((r) => setTimeout(r, 500));
    }
    return false;
  }, timeout);
}
