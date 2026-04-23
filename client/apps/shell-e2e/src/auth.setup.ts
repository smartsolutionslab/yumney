import { test as setup, expect } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';
const KEYCLOAK_URL = process.env['KEYCLOAK_URL'] ?? 'http://localhost:8080';
const KEYCLOAK_REALM = 'yumney';
const KEYCLOAK_CLIENT = 'yumney-web';
const AUTH_STATE_PATH = 'src/.auth/user.json';

setup.setTimeout(60_000);

// Bypass the OIDC redirect flow entirely. Hit Keycloak's token endpoint
// directly via password grant (the yumney-web test client has
// directAccessGrantsEnabled=true) and plant the tokens into sessionStorage in
// the shape angular-oauth2-oidc expects, then save the storageState for
// downstream specs.
//
// Rationale: the full redirect-driven flow proved fragile inside Aspire's DCP
// localhost proxy on GitHub runners — browser navigations to Keycloak were
// silently aborted (net::ERR_ABORTED) and the Keycloak container never saw the
// request. Since we're testing the authenticated app, not the OIDC protocol,
// jumping straight to "already authenticated" is both simpler and robust.
setup('authenticate via Keycloak direct grant', async ({ page, request }) => {
  // 1. Obtain tokens directly.
  const tokenResponse = await request.post(
    `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`,
    {
      form: {
        grant_type: 'password',
        client_id: KEYCLOAK_CLIENT,
        username: E2E_USER,
        password: E2E_PASSWORD,
        scope: 'openid profile email roles',
      },
      timeout: 30_000,
    },
  );
  expect(tokenResponse.ok(), `Keycloak token endpoint returned ${tokenResponse.status()}`).toBe(
    true,
  );
  const tokens = (await tokenResponse.json()) as {
    access_token: string;
    id_token: string;
    refresh_token?: string;
    expires_in: number;
    scope?: string;
  };

  // 2. Decode id_token claims for angular-oauth2-oidc's id_token_claims_obj.
  const idClaims = decodeJwtPayload(tokens.id_token);
  const now = Date.now();
  const expiresAt = (now + tokens.expires_in * 1000).toString();
  const idTokenExpiresAt = (
    typeof idClaims['exp'] === 'number' ? idClaims['exp'] * 1000 : now + tokens.expires_in * 1000
  ).toString();

  // 3. Load the app shell once so we can write sessionStorage for its origin.
  //    domcontentloaded is enough here — we're not interacting with the app,
  //    just scribbling into its storage before the next test opens the page.
  await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 60_000 });

  // 4. Plant the entries angular-oauth2-oidc reads on startup.
  await page.evaluate(
    ({ accessToken, idToken, refreshToken, expiresAt, idTokenExpiresAt, idClaims, scope }) => {
      const s = sessionStorage;
      s.setItem('access_token', accessToken);
      s.setItem('access_token_stored_at', String(Date.now()));
      s.setItem('expires_at', expiresAt);
      s.setItem('id_token', idToken);
      s.setItem('id_token_claims_obj', JSON.stringify(idClaims));
      s.setItem('id_token_expires_at', idTokenExpiresAt);
      s.setItem('id_token_stored_at', String(Date.now()));
      if (refreshToken) s.setItem('refresh_token', refreshToken);
      s.setItem('granted_scopes', JSON.stringify((scope ?? '').split(/\s+/).filter(Boolean)));
    },
    {
      accessToken: tokens.access_token,
      idToken: tokens.id_token,
      refreshToken: tokens.refresh_token,
      expiresAt,
      idTokenExpiresAt,
      idClaims,
      scope: tokens.scope,
    },
  );

  await page.context().storageState({ path: AUTH_STATE_PATH });
});

function decodeJwtPayload(token: string): Record<string, unknown> {
  const [, payload] = token.split('.');
  if (!payload) return {};
  const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
  return JSON.parse(Buffer.from(padded, 'base64').toString('utf8')) as Record<string, unknown>;
}
