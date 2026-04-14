import { AuthConfig } from 'angular-oauth2-oidc';

export interface AppConfig {
  keycloakUrl: string;
  keycloakRealm: string;
  keycloakClientId: string;
  gatewayUrl?: string;
}

export function createAuthConfig(
  keycloakUrl: string,
  realm: string,
  clientId: string,
  gatewayUrl?: string,
): AuthConfig {
  const realmUrl = `${keycloakUrl}/realms/${realm}`;
  const tokenBaseUrl = gatewayUrl ?? keycloakUrl;
  return {
    issuer: realmUrl,
    clientId,
    responseType: 'code',
    scope: 'openid profile email roles',
    redirectUri: typeof window !== 'undefined' ? window.location.origin : '',
    postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : '',
    tokenEndpoint: `${tokenBaseUrl}/realms/${realm}/protocol/openid-connect/token`,
    requireHttps: false,
    strictDiscoveryDocumentValidation: false,
  };
}
