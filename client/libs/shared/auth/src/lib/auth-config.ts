import { AuthConfig } from 'angular-oauth2-oidc';

export interface AppConfig {
  keycloakUrl: string;
  keycloakRealm: string;
  keycloakClientId: string;
}

export function createAuthConfig(keycloakUrl: string, realm: string, clientId: string): AuthConfig {
  return {
    issuer: `${keycloakUrl}/realms/${realm}`,
    clientId,
    responseType: 'code',
    scope: 'openid profile email roles',
    redirectUri: typeof window !== 'undefined' ? window.location.origin : '',
    postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : '',
    requireHttps: false,
    strictDiscoveryDocumentValidation: false,
  };
}
