import { test as setup } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';
const KEYCLOAK_URL = process.env['KEYCLOAK_URL'] ?? 'http://localhost:8080';
const KEYCLOAK_REALM = 'yumney';
const KEYCLOAK_CLIENT_ID = 'yumney-web';
const BASE_URL = process.env['BASE_URL'] ?? 'http://localhost:4200';

const AUTH_STATE_PATH = 'src/.auth/user.json';

/**
 * Playwright setup: gets a Keycloak token via direct grant,
 * loads the app to trigger discovery document fetch,
 * injects the tokens, reloads to authenticate, then saves state.
 */
setup('authenticate via Keycloak token endpoint', async ({ page }) => {
  // 1. Get tokens from Keycloak
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

  // 2. Load the app to trigger OIDC discovery document fetch
  await page.goto(BASE_URL);
  await page.waitForLoadState('load');

  // 3. Inject tokens into sessionStorage (angular-oauth2-oidc keys)
  const expiresAt = Math.floor(Date.now() / 1000) + tokens.expires_in;
  const idClaims = JSON.parse(atob(tokens.id_token.split('.')[1]));

  await page.evaluate(
    ({ t, exp, claims }) => {
      const store = sessionStorage;
      store.setItem('access_token', t.access_token);
      store.setItem('id_token', t.id_token);
      store.setItem('refresh_token', t.refresh_token);
      store.setItem('expires_at', String(exp));
      store.setItem('granted_scopes', JSON.stringify(t.scope.split(' ')));
      store.setItem('access_token_stored_at', String(Date.now()));
      store.setItem('id_token_stored_at', String(Date.now()));
      store.setItem('id_token_claims_obj', JSON.stringify(claims));
      store.setItem('id_token_expires_at', String(exp));
      store.setItem('nonce', claims.nonce || '');
      store.setItem('PKCE_verifier', '');
    },
    { t: tokens, exp: expiresAt, claims: idClaims },
  );

  // 4. Reload so the app picks up the injected tokens
  await page.reload();
  await page.waitForLoadState('load');

  // 5. Wait briefly for Angular to process the tokens
  await page.waitForTimeout(2000);

  // 6. Save authenticated state
  await page.context().storageState({ path: AUTH_STATE_PATH });
});
