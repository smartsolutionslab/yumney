import { Injectable, signal, computed, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { OAuthService } from 'angular-oauth2-oidc';
import { AppConfig, createAuthConfig } from './auth-config';
import { REMEMBER_ME_KEY } from './auth-storage.factory';
import type { AuthUser } from './auth-user';

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

  constructor(private oauthService: OAuthService) {}

  async initialize(): Promise<void> {
    const g = globalThis as Record<string, unknown>;
    const mark = (name: string) => {
      g[`__ynAuth_${name}`] = Date.now();
      console.log(`[auth] ${name}`);
    };
    mark('enter');

    const { keycloakUrl, keycloakRealm, keycloakClientId, gatewayUrl } = await this.loadAppConfig();
    mark('afterLoadAppConfig');

    const authConfig = createAuthConfig(keycloakUrl, keycloakRealm, keycloakClientId, gatewayUrl);

    this.oauthService.configure(authConfig);
    mark('afterConfigure');

    this.oauthService.setupAutomaticSilentRefresh();
    mark('afterSetupSilentRefresh');

    this.oauthService.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.updateAuthState();
    });
    mark('afterEventsSub');

    try {
      await this.oauthService.loadDiscoveryDocument();
      mark('afterLoadDiscoveryDocument');
      // Override token endpoint to route through Gateway (avoids DCP port proxy 504s)
      if (gatewayUrl) {
        this.oauthService.tokenEndpoint = `${gatewayUrl}/realms/${keycloakRealm}/protocol/openid-connect/token`;
      }
      await this.oauthService.tryLogin();
      mark('afterTryLogin');
      this.updateAuthState();
    } catch (err) {
      // Keycloak unreachable — app continues unauthenticated
      mark('caughtError');
      console.warn('[auth] caughtError details:', err);
    } finally {
      this.isLoading.set(false);
      mark('finally');
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
