import { AuthConfig } from 'angular-oauth2-oidc';

export interface AppConfig {
  keycloakUrl: string;
  keycloakRealm: string;
  keycloakClientId: string;
  gatewayUrl?: string;
}

export function createAuthConfig(keycloakUrl: string, realm: string, clientId: string, gatewayUrl?: string): AuthConfig {
  const realmUrl = `${keycloakUrl}/realms/${realm}`;
  const tokenBaseUrl = gatewayUrl ?? keycloakUrl;
  return {
    issuer: realmUrl,
    clientId,
    responseType: 'code',
    scope: 'openid profile email roles',
    redirectUri: typeof window !== 'undefined' ? window.location.origin : '',
    postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : '',
    // Explicit endpoints avoid depending on the OIDC discovery document at
    // runtime. Keycloak's URL shape is stable, so deriving the endpoints
    // deterministically is safe and sidesteps environments where the
    // discovery fetch stalls (e.g. when Keycloak's advertised jwks_uri
    // points at an internal hostname the browser can't resolve).
    loginUrl: `${realmUrl}/protocol/openid-connect/auth`,
    tokenEndpoint: `${tokenBaseUrl}/realms/${realm}/protocol/openid-connect/token`,
    userinfoEndpoint: `${realmUrl}/protocol/openid-connect/userinfo`,
    logoutUrl: `${realmUrl}/protocol/openid-connect/logout`,
    requireHttps: false,
    strictDiscoveryDocumentValidation: false,
    skipIssuerCheck: true,
  };
}
