import { test as setup, expect } from '@playwright/test';

const E2E_USER = process.env['E2E_USER'] ?? 'testuser';
const E2E_PASSWORD = process.env['E2E_PASSWORD'] ?? 'Test1234';
// Route Keycloak traffic through the Yumney Gateway (YARP) rather than the
// direct container port. Hitting localhost:8080 from either the browser or
// Playwright's Node request context reliably hangs on CI — Aspire's DCP
// localhost-proxy appears to drop that particular combination. Gateway port
// 5100 is a plain ASP.NET endpoint that YARP then routes to the keycloak
// service via Aspire service discovery (container-network hop), which works.
// This also matches how the production Angular code routes token exchanges
// (see AuthService.initialize overriding oauthService.tokenEndpoint).
const GATEWAY_URL = process.env['GATEWAY_URL'] ?? 'http://localhost:5100';
const KEYCLOAK_REALM = 'yumney';
const KEYCLOAK_CLIENT = 'yumney-web';
const AUTH_STATE_PATH = 'src/.auth/user.json';

setup.setTimeout(180_000);

setup('authenticate via Keycloak direct grant', async ({ page, request }) => {
  // 1. Obtain tokens directly (through the Gateway, not direct Keycloak).
  //    The wait-for-services poll observed Keycloak's first 200 response
  //    taking ~65s on a cold CI runner; the token endpoint is similarly
  //    cold on first real hit. Give it 120s headroom so we don't give up
  //    before Keycloak warms up.
  const tokenResponse = await request.post(
    `${GATEWAY_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`,
    {
      form: {
        grant_type: 'password',
        client_id: KEYCLOAK_CLIENT,
        username: E2E_USER,
        password: E2E_PASSWORD,
        scope: 'openid profile email roles',
      },
      timeout: 120_000,
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
