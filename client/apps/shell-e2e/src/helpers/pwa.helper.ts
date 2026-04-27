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
 * Wait until the page is being controlled by an active service worker AND
 * the NGSW asset cache actually contains the URLs the offline tests rely
 * on. NGSW prefetches the `app` asset group asynchronously after
 * activation, so even after `controller` is set there's a window where
 * caches.match('/index.html') returns nothing. Without this, setOffline
 * + reload races the prefetch and serves nothing from cache.
 *
 * Throws — loudly — if the SW never controls the page or if the index.html
 * cache prime never lands. Previously the helper silently returned on
 * timeout, which masked PWA test failures (#421): the offline tests would
 * race an empty cache and fail with `yn-root not attached`, with no
 * indication that the upstream priming had given up.
 *
 * Time budget is split: half for controller activation, half for cache
 * priming. A single shared deadline meant a slow activation could starve
 * the cache prime entirely.
 */
export async function waitForServiceWorkerControl(page: Page, timeout = 120_000): Promise<void> {
  const result = await page.evaluate(async (totalMs) => {
    if (!('serviceWorker' in navigator)) {
      return { ok: true as const, reason: 'no-service-worker-support' };
    }

    // 25% of budget for activation, 75% for prefetch — busy CI runners
    // routinely take 30-60s to finish NGSW's prefetch over many JS chunks.
    const controllerBudget = Math.floor(totalMs * 0.25);
    const cacheBudget = totalMs - controllerBudget;

    // Phase 1: wait for the SW to control fetches.
    const controllerDeadline = Date.now() + controllerBudget;
    while (Date.now() < controllerDeadline && !navigator.serviceWorker.controller) {
      await new Promise((r) => setTimeout(r, 100));
    }
    if (!navigator.serviceWorker.controller) {
      const regs = await navigator.serviceWorker.getRegistrations();
      return {
        ok: false as const,
        reason: 'controller-never-set',
        elapsedMs: controllerBudget,
        registrations: regs.length,
        states: regs.map((r) => ({
          active: r.active?.state ?? null,
          installing: r.installing?.state ?? null,
          waiting: r.waiting?.state ?? null,
        })),
      };
    }

    // Phase 2: prime the cache for the URLs the offline tests care about.
    // fetch() routes through the controlling SW; NGSW satisfies from cache
    // or falls through to network and stores. We accept either /index.html
    // or / as the SPA shell anchor — NGSW's navigationUrls fallback may
    // store the shell under either depending on how the navigation was made.
    const warmupUrls = ['/', '/index.html', '/assets/i18n/en.json'];
    const fetchResults = await Promise.all(
      warmupUrls.map(async (url) => {
        try {
          const res = await fetch(url);
          return { url, status: res.status };
        } catch (err) {
          return { url, status: 0, error: String(err) };
        }
      }),
    );

    const isShellPath = (p: string): boolean => p === '/index.html' || p === '/';

    const cacheDeadline = Date.now() + cacheBudget;
    while (Date.now() < cacheDeadline) {
      const indexHit = await caches.match('/index.html');
      const rootHit = await caches.match('/');
      if (indexHit || rootHit) {
        return { ok: true as const, reason: 'cache-primed' };
      }
      await new Promise((r) => setTimeout(r, 250));
    }

    const cacheNames = await caches.keys();
    const cacheDetails: Array<{
      name: string;
      size: number;
      sample: string[];
      hasShell: boolean;
    }> = [];
    for (const name of cacheNames) {
      const cache = await caches.open(name);
      const keys = await cache.keys();
      const paths = keys.map((req) => new URL(req.url).pathname);
      cacheDetails.push({
        name,
        size: keys.length,
        sample: paths.slice(0, 8),
        hasShell: paths.some(isShellPath),
      });
    }
    return {
      ok: false as const,
      reason: 'spa-shell-never-cached',
      elapsedMs: totalMs,
      fetchResults,
      cacheDetails,
    };
  }, timeout);

  if (!result.ok) {
    throw new Error(
      `waitForServiceWorkerControl failed: ${result.reason}\n` +
        `details: ${JSON.stringify(result, null, 2)}`,
    );
  }
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
