import { test as base, type Page } from '@playwright/test';

/**
 * Mocks the OIDC discovery and token endpoints so the app's AuthService
 * initializes with a valid authenticated session — no real Keycloak needed.
 */
async function mockOidcAuth(page: Page): Promise<void> {
  const keycloakUrl = 'http://localhost:8080';
  const realm = 'yumney';
  const issuer = `${keycloakUrl}/realms/${realm}`;

  // Mock the app config
  await page.route('**/assets/config/app-config.json', (route) =>
    route.fulfill({
      status: 200,
      json: { keycloakUrl, keycloakRealm: realm, keycloakClientId: 'yumney-web' },
    }),
  );

  // Mock OIDC discovery document
  await page.route(`${issuer}/.well-known/openid-configuration`, (route) =>
    route.fulfill({
      status: 200,
      json: {
        issuer,
        authorization_endpoint: `${issuer}/protocol/openid-connect/auth`,
        token_endpoint: `${issuer}/protocol/openid-connect/token`,
        userinfo_endpoint: `${issuer}/protocol/openid-connect/userinfo`,
        end_session_endpoint: `${issuer}/protocol/openid-connect/logout`,
        jwks_uri: `${issuer}/protocol/openid-connect/certs`,
        response_types_supported: ['code'],
        subject_types_supported: ['public'],
        id_token_signing_alg_values_supported: ['RS256'],
      },
    }),
  );

  // Mock JWKS (empty keys — we won't validate signatures in E2E)
  await page.route(`${issuer}/protocol/openid-connect/certs`, (route) =>
    route.fulfill({ status: 200, json: { keys: [] } }),
  );

  // Inject mock tokens into sessionStorage before the app loads
  const mockClaims = {
    sub: 'e2e-user-id',
    email: 'e2e@yumney.dev',
    preferred_username: 'E2E User',
    realm_access: { roles: ['user'] },
    iss: issuer,
    aud: 'yumney-web',
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000),
  };

  const mockIdToken = `eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.${btoa(JSON.stringify(mockClaims))}.`;
  const mockAccessToken = mockIdToken;

  await page.addInitScript(
    ({ accessToken, idToken, claims }) => {
      sessionStorage.setItem('access_token', accessToken);
      sessionStorage.setItem('id_token', idToken);
      sessionStorage.setItem('id_token_claims_obj', JSON.stringify(claims));
      sessionStorage.setItem('access_token_stored_at', String(Date.now()));
      sessionStorage.setItem('id_token_stored_at', String(Date.now()));
      sessionStorage.setItem('expires_at', String(Date.now() + 3600000));
      sessionStorage.setItem('granted_scopes', 'openid profile email roles');
    },
    { accessToken: mockAccessToken, idToken: mockIdToken, claims: mockClaims },
  );
}

export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ page }, use) => {
    await mockOidcAuth(page);
    await use(page);
  },
});

export { expect } from '@playwright/test';
