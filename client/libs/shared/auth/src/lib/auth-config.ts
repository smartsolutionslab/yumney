import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
  issuer: 'http://localhost:8080/realms/yumney',
  clientId: 'yumney-web',
  responseType: 'code',
  scope: 'openid profile email roles',
  redirectUri: typeof window !== 'undefined' ? window.location.origin : '',
  postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : '',
  requireHttps: false,
  strictDiscoveryDocumentValidation: false,
};
