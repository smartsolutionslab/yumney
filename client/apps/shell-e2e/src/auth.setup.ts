import { test as setup } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';
const KEYCLOAK_URL = process.env['KEYCLOAK_URL'] ?? 'http://localhost:8080';
const KEYCLOAK_REALM = 'yumney';
const KEYCLOAK_CLIENT_ID = 'yumney-web';

const AUTH_STATE_PATH = 'src/.auth/user.json';

setup('authenticate via Keycloak', async ({ page }) => {
  const tokenUrl = `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`;
  const tokenResponse = await fetch(tokenUrl, {
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

  if (!tokenResponse.ok) {
    throw new Error(`Keycloak token request failed (${tokenResponse.status})`);
  }

  const tokens = await tokenResponse.json();
  const idClaims = JSON.parse(atob(tokens.id_token.split('.')[1]));
  const expiresAt = Math.floor(Date.now() / 1000) + tokens.expires_in;

  // Inject into BOTH localStorage and sessionStorage — cover both cases
  await page.addInitScript(
    ({ t, exp, claims }) => {
      const stores = [localStorage, sessionStorage];
      for (const store of stores) {
        store.setItem('yn_remember_me', 'true');
        store.setItem('access_token', t.access_token);
        store.setItem('id_token', t.id_token);
        store.setItem('refresh_token', t.refresh_token);
        store.setItem('expires_at', String(exp));
        store.setItem('id_token_expires_at', String(exp));
        store.setItem('access_token_stored_at', String(Date.now()));
        store.setItem('id_token_stored_at', String(Date.now()));
        store.setItem('id_token_claims_obj', JSON.stringify(claims));
        store.setItem('granted_scopes', JSON.stringify(t.scope.split(' ')));
        store.setItem('nonce', '');
        store.setItem('PKCE_verifier', '');
        store.setItem('session_state', t.session_state || '');
      }
    },
    { t: tokens, exp: expiresAt, claims: idClaims },
  );

  await page.goto('/dashboard');
  await page.waitForLoadState('load');
  await page.waitForTimeout(5000);

  console.log('Final URL:', page.url());
  await page.context().storageState({ path: AUTH_STATE_PATH });
});
