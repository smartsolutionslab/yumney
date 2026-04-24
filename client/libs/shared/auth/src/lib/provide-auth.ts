import { APP_INITIALIZER, EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { provideOAuthClient, OAuthStorage } from 'angular-oauth2-oidc';
import { AppConfigService } from './app-config.service';
import { AuthService } from './auth.service';
import { authStorageFactory } from './auth-storage.factory';

function initializeAuth(
  appConfig: AppConfigService,
  authService: AuthService,
): () => Promise<void> {
  // Load app-config.json first so AuthService and apiBaseInterceptor both
  // see the resolved gatewayUrl at runtime. Sequencing matters: if a
  // component fires an HTTP request during AuthService.initialize(), the
  // interceptor needs gatewayUrl already populated.
  return async () => {
    await appConfig.load();
    await authService.initialize();
  };
}

export function provideAuth(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideOAuthClient(),
    { provide: OAuthStorage, useFactory: authStorageFactory },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AppConfigService, AuthService],
      multi: true,
    },
  ]);
}
