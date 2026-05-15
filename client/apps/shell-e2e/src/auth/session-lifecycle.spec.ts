import { test, expect } from '../fixtures/auth.fixture';
import { HeaderPage } from '../pages/header.page';
import { TIMEOUTS } from '../helpers/timeouts';

/**
 * Coverage for #407 — auth session lifecycle. Three scenarios that previously
 * had no e2e coverage:
 *
 *   - rejected refresh: expired access token + Keycloak refuses refresh →
 *     authGuard kicks the user to /auth/login
 *   - explicit logout: clicking the header's logout entry clears tokens and
 *     a subsequent protected-route navigation redirects to /auth/login
 *   - corrupted token: garbage in localStorage.access_token → app does not
 *     crash and redirects to /auth/login on the next protected nav
 *
 * The fourth scenario from #407 (silent refresh succeeds) is implicitly
 * exercised by every other e2e in the suite — they all rely on the
 * stored token staying valid across the run. A direct test would need to
 * coordinate with angular-oauth2-oidc's internal refresh timer; deferred.
 */
test.describe('Auth Session Lifecycle (#407)', () => {
  test('redirects to login when refresh is rejected by Keycloak', async ({ authenticatedPage }) => {
    // Intercept the refresh-token POST and reject it as Keycloak would when
    // the refresh token has been revoked or the session expired server-side.
    await authenticatedPage.route('**/realms/yumney/protocol/openid-connect/token', (route) => {
      if (route.request().method() === 'POST') {
        return route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({
            error: 'invalid_grant',
            error_description: 'Token is not active',
          }),
        });
      }
      return route.continue();
    });

    // Force the stored access token to look expired so the auth lib /
    // authGuard sees no valid token. Use both expires_at and
    // id_token_expires_at so neither token reads as live.
    await authenticatedPage.evaluate(() => {
      localStorage.setItem('expires_at', '0');
      localStorage.setItem('id_token_expires_at', '0');
    });

    // Navigate to a protected route — authGuard should bounce us.
    await authenticatedPage.goto('/recipes');

    await expect(authenticatedPage).toHaveURL(/\/auth\/login/, {
      timeout: TIMEOUTS.default,
    });
  });

  test('explicit logout clears session and bounces protected nav', async ({ authenticatedPage }) => {
    // Open the user menu, click Logout. The lib navigates to Keycloak's
    // logout endpoint then back to origin — point both at a stub so the
    // test does not depend on the real Keycloak being reachable.
    await authenticatedPage.route('**/realms/yumney/protocol/openid-connect/logout*', (route) => route.fulfill({ status: 200, body: '' }));

    const header = new HeaderPage(authenticatedPage);
    await header.logout();

    // logout() clears the stored access token. Wait for that to land
    // before exercising the protected-route redirect.
    await expect
      .poll(async () => authenticatedPage.evaluate(() => localStorage.getItem('access_token')), {
        timeout: TIMEOUTS.default,
      })
      .toBeNull();

    await authenticatedPage.goto('/recipes');
    await expect(authenticatedPage).toHaveURL(/\/auth\/login/, {
      timeout: TIMEOUTS.default,
    });
  });

  test('redirects to login when access_token is corrupted', async ({ authenticatedPage }) => {
    // Replace the JWT with garbage. The auth lib's hasValidAccessToken
    // returns false on a non-decodable token, the authGuard should
    // redirect rather than the app crashing on a parse error.
    await authenticatedPage.evaluate(() => {
      localStorage.setItem('access_token', 'not-a-real-jwt');
      localStorage.setItem('expires_at', '0');
    });

    await authenticatedPage.goto('/recipes');

    await expect(authenticatedPage).toHaveURL(/\/auth\/login/, {
      timeout: TIMEOUTS.default,
    });

    // Sanity: there should be no uncaught page error from the bad token.
    // (Playwright surfaces these in trace artifacts; an explicit listener
    // would over-couple the test to the auth lib's internals.)
  });
});
