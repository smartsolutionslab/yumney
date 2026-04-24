import { Injectable } from '@angular/core';
import type { AppConfig } from './auth-config';

const DEFAULT_CONFIG: AppConfig = {
  keycloakUrl: 'http://localhost:8080',
  keycloakRealm: 'yumney',
  keycloakClientId: 'yumney-web',
};

/**
 * Loads `/assets/config/app-config.json` once at bootstrap and exposes the
 * result to the rest of the app. Kept in the auth lib because it already
 * owns the AppConfig type; promote to its own lib if more non-auth consumers
 * show up.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private config: AppConfig = DEFAULT_CONFIG;

  async load(): Promise<void> {
    try {
      const response = await fetch('/assets/config/app-config.json');
      if (response.ok) {
        this.config = (await response.json()) as AppConfig;
      }
    } catch {
      // Config file unavailable — keep defaults.
    }
  }

  get(): AppConfig {
    return this.config;
  }

  /** Gateway base URL (no trailing slash). Falls back to empty string when
   * not configured — relative `/api/*` URLs then hit the current origin. */
  get gatewayUrl(): string {
    return (this.config.gatewayUrl ?? '').replace(/\/+$/, '');
  }
}
