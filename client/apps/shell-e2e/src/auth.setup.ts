import { test as setup, expect } from '@playwright/test';
import { readFileSync } from 'node:fs';

const AUTH_STATE_PATH = 'src/.auth/user.json';
const TOKENS_FILE = process.env['E2E_TOKENS_FILE'];

setup.setTimeout(60_000);

// The CI network (Aspire DCP + GitHub runner) has been a nightmare for any
// in-process HTTP client trying to reach Keycloak: browser navigations abort
// (net::ERR_ABORTED), Playwright's Node request context times out, YARP
// routing returns 504, even curl to localhost:8080 (DCP proxy port) often
// stalls. What reliably works is curl directly to the Docker-mapped host
// port for Keycloak's container.
//
// So the e2e.yml workflow discovers that port via `docker port`, fetches
// the token via password grant, and writes the JSON to /tmp/e2e-tokens.json.
// This test reads it and plants the tokens into sessionStorage in the keys
// angular-oauth2-oidc reads on startup.
setup('authenticate via pre-fetched Keycloak tokens', async ({ page }) => {
  expect(TOKENS_FILE, 'E2E_TOKENS_FILE env var must point at a JSON token dump').toBeDefined();

  const tokens = JSON.parse(readFileSync(TOKENS_FILE as string, 'utf8')) as {
    access_token: string;
    id_token: string;
    refresh_token?: string;
    expires_in: number;
    scope?: string;
  };

  // Decode id_token claims for angular-oauth2-oidc's id_token_claims_obj.
  const idClaims = decodeJwtPayload(tokens.id_token);
  const now = Date.now();
  const expiresAt = (now + tokens.expires_in * 1000).toString();
  const idTokenExpiresAt = (
    typeof idClaims['exp'] === 'number' ? idClaims['exp'] * 1000 : now + tokens.expires_in * 1000
  ).toString();

  // Navigate to the origin once so sessionStorage is scoped correctly.
  await page.goto('/', { waitUntil: 'domcontentloaded', timeout: 60_000 });

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
