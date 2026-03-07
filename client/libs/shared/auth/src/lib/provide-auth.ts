import { APP_INITIALIZER, EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { provideOAuthClient, OAuthStorage } from 'angular-oauth2-oidc';
import { AuthService } from './auth.service';
import { authStorageFactory } from './auth-storage.factory';

function initializeAuth(authService: AuthService): () => Promise<void> {
  return () => authService.initialize();
}

export function provideAuth(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideOAuthClient(),
    { provide: OAuthStorage, useFactory: authStorageFactory },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthService],
      multi: true,
    },
  ]);
}
