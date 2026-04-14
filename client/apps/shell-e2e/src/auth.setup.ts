import { test as setup } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';
const KEYCLOAK_URL = process.env['KEYCLOAK_URL'] ?? 'http://localhost:8080';
const KEYCLOAK_REALM = 'yumney';
const KEYCLOAK_CLIENT_ID = 'yumney-web';

const AUTH_STATE_PATH = 'src/.auth/user.json';

/**
 * Playwright setup project: obtains a Keycloak token via the
 * Resource Owner Password Credentials grant (direct API call,
 * no browser needed), then injects the tokens into sessionStorage
 * so angular-oauth2-oidc recognises them. Saves the browser state
 * for all test projects to reuse.
 */
setup('authenticate via Keycloak token endpoint', async ({ page }) => {
  const tokenUrl = `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`;

  const response = await fetch(tokenUrl, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'password',
      client_id: KEYCLOAK_CLIENT_ID,
      username: E2E_USER,
      password: E2E_PASSWORD,
      scope: 'openid profile email roles',
    }),
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`Keycloak token request failed (${response.status}): ${body}`);
  }

  const tokens = await response.json();

  // Navigate to the app origin so we can set sessionStorage on the correct domain
  await page.goto('/');
  await page.waitForLoadState('domcontentloaded');

  // Inject tokens into sessionStorage — angular-oauth2-oidc storage keys
  const now = Math.floor(Date.now() / 1000);
  const expiresAt = now + tokens.expires_in;

  await page.evaluate(
    ({ tokens: t, expiresAt: exp }) => {
      sessionStorage.setItem('access_token', t.access_token);
      sessionStorage.setItem('id_token', t.id_token);
      sessionStorage.setItem('refresh_token', t.refresh_token);
      sessionStorage.setItem('expires_at', String(exp));
      sessionStorage.setItem('granted_scopes', JSON.stringify(t.scope.split(' ')));
      sessionStorage.setItem('access_token_stored_at', String(Date.now()));
      sessionStorage.setItem('id_token_stored_at', String(Date.now()));
      sessionStorage.setItem(
        'id_token_claims_obj',
        JSON.stringify(JSON.parse(atob(t.id_token.split('.')[1]))),
      );
      sessionStorage.setItem('PKCE_verifier', '');
    },
    { tokens, expiresAt },
  );

  // Save the authenticated browser state for reuse
  await page.context().storageState({ path: AUTH_STATE_PATH });
});
