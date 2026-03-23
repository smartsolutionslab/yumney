import { Injectable, signal, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OAuthService } from 'angular-oauth2-oidc';
import { AppConfig, createAuthConfig } from './auth-config';
import { REMEMBER_ME_KEY } from './auth-storage.factory';

export interface AuthUser {
  sub: string;
  email: string;
  preferredUsername: string;
  roles: string[];
}

const DEFAULT_CONFIG: AppConfig = {
  keycloakUrl: 'http://localhost:8080',
  keycloakRealm: 'yumney',
  keycloakClientId: 'yumney-web',
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  isLoading = signal(true);
  isAuthenticated = signal(false);
  currentUser = signal<AuthUser | null>(null);
  displayName = computed(
    () => this.currentUser()?.preferredUsername ?? this.currentUser()?.email ?? null,
  );

  private destroyRef = inject(DestroyRef);

  constructor(private oauthService: OAuthService) {}

  async initialize(): Promise<void> {
    const appConfig = await this.loadAppConfig();
    const authConfig = createAuthConfig(
      appConfig.keycloakUrl,
      appConfig.keycloakRealm,
      appConfig.keycloakClientId,
    );

    this.oauthService.configure(authConfig);
    this.oauthService.setupAutomaticSilentRefresh();

    this.oauthService.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.updateAuthState();
    });

    try {
      await this.oauthService.loadDiscoveryDocumentAndTryLogin();
      this.updateAuthState();
    } catch {
      // Keycloak unreachable — app continues unauthenticated
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

  forgotPassword(): void {
    this.oauthService.initCodeFlow('', { kc_action: 'UPDATE_PASSWORD' });
  }

  private async loadAppConfig(): Promise<AppConfig> {
    try {
      const response = await fetch('/assets/config/app-config.json');
      if (response.ok) {
        return await response.json();
      }
    } catch {
      // Config file unavailable — use defaults
    }
    return DEFAULT_CONFIG;
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
