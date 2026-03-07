import { OAuthStorage } from 'angular-oauth2-oidc';

export const REMEMBER_ME_KEY = 'yn_remember_me';

export function authStorageFactory(): OAuthStorage {
  if (typeof localStorage !== 'undefined' && localStorage.getItem(REMEMBER_ME_KEY) === 'true') {
    return localStorage;
  }
  return sessionStorage;
}
