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
 * Wait for the service worker to become activated.
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
