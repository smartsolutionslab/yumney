import { Injectable, signal, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OAuthService } from 'angular-oauth2-oidc';
import { createAuthConfig } from './auth-config';
import { AppConfigService } from './app-config.service';
import { REMEMBER_ME_KEY } from './auth-storage.factory';
import type { AuthUser } from './auth-user';

async function withTimeout<T>(promise: Promise<T>, ms: number, label: string): Promise<T> {
  let timer: ReturnType<typeof setTimeout> | undefined;
  const timeout = new Promise<never>((_, reject) => {
    timer = setTimeout(() => reject(new Error(`${label} timed out after ${ms}ms`)), ms);
  });
  try {
    return await Promise.race([promise, timeout]);
  } finally {
    clearTimeout(timer);
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  isLoading = signal(true);
  isAuthenticated = signal(false);
  currentUser = signal<AuthUser | null>(null);
  displayName = computed(() => this.currentUser()?.preferredUsername ?? this.currentUser()?.email ?? null);
  shortName = computed(() => {
    const name = this.displayName();
    if (!name) return null;
    const atIndex = name.indexOf('@');
    const [first] = name.split(' ');
    return atIndex > 0 ? name.substring(0, atIndex) : first;
  });
  userInitial = computed(() => {
    const name = this.shortName();
    return name ? name[0].toUpperCase() : null;
  });

  private destroyRef = inject(DestroyRef);
  private appConfig = inject(AppConfigService);

  constructor(private oauthService: OAuthService) {}

  async initialize(): Promise<void> {
    // AppConfigService is loaded via APP_INITIALIZER before this runs, so
    // reading synchronously is safe.
    const { keycloakUrl, keycloakRealm, keycloakClientId, gatewayUrl } = this.appConfig.get();
    const authConfig = createAuthConfig(keycloakUrl, keycloakRealm, keycloakClientId, gatewayUrl);

    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();

    this.oauthService.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.updateAuthState();
    });

    try {
      // createAuthConfig already sets loginUrl / tokenEndpoint / userinfoEndpoint
      // / logoutUrl / jwksUri deterministically from the Keycloak URL + realm,
      // so we don't need the OIDC discovery document to run the flow. The
      // previous loadDiscoveryDocument() call hung indefinitely in CI (HTTP
      // fetch succeeded with 200, but something downstream — likely JWKS —
      // never resolved), which stalled APP_INITIALIZER and blocked
      // bootstrapApplication. tryLogin() also wrapped in a timeout as belt &
      // suspenders in case the cookie-based session lookup hangs.
      await withTimeout(this.oauthService.tryLogin(), 10_000, 'tryLogin');
      this.updateAuthState();
    } catch (err) {
      // Keycloak unreachable or tryLogin stalled — app continues
      // unauthenticated. Logging the cause helps future CI debugging.
      console.warn('[auth] initialize failed:', err);
    } finally {
      this.isLoading.set(false);
    }
  }

  login(rememberMe = false): void {
    if (rememberMe) {
      localStorage.setItem(REMEMBER_ME_KEY, 'true');
    } else {
      localStorage.removeItem(REMEMBER_ME_KEY);
    }
    this.oauthService.initCodeFlow();
  }

  logout(): void {
    this.oauthService.logOut();
    this.isAuthenticated.set(false);
    this.currentUser.set(null);
    localStorage.removeItem(REMEMBER_ME_KEY);
  }

  getAccessToken(): string | null {
    return this.oauthService.hasValidAccessToken() ? this.oauthService.getAccessToken() : null;
  }

  forgotPassword(): void {
    this.oauthService.initCodeFlow('', { kc_action: 'UPDATE_PASSWORD' });
  }

  private updateAuthState(): void {
    const hasValidToken = this.oauthService.hasValidAccessToken();
    this.isAuthenticated.set(hasValidToken);

    if (hasValidToken) {
      const claims = this.oauthService.getIdentityClaims();
      if (claims) {
        const { sub, email, preferred_username, realm_access } = claims;
        this.currentUser.set({
          sub,
          email,
          preferredUsername: preferred_username,
          roles: realm_access?.['roles'] ?? [],
        });
      }
    } else {
      this.currentUser.set(null);
    }
  }
}
