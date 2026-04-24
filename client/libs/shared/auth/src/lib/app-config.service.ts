import { Injectable } from '@angular/core';
import type { AppConfig } from './auth-config';

const DEFAULT_CONFIG: AppConfig = {
  keycloakUrl: 'http://localhost:8080',
  keycloakRealm: 'yumney',
  keycloakClientId: 'yumney-web',
};

const GLOBAL_KEY = '__yumneyAppConfig';

interface GlobalAppConfigHost {
  [GLOBAL_KEY]?: AppConfig;
}

function globalHost(): GlobalAppConfigHost | undefined {
  return typeof globalThis === 'undefined' ? undefined : (globalThis as GlobalAppConfigHost);
}

/**
 * Loads `/assets/config/app-config.json` once at bootstrap and stores the
 * result on globalThis so every federated MFE reads the same value — even
 * ones whose bundles hold their own copy of this class (native-federation
 * bundles workspace libs per-MFE, so DI singletons break across bundles).
 * Interceptors read via {@link getAppConfigGatewayUrl} which bypasses DI
 * entirely.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  async load(): Promise<void> {
    try {
      const response = await fetch('/assets/config/app-config.json');
      if (response.ok) {
        const cfg = (await response.json()) as AppConfig;
        const host = globalHost();
        if (host) host[GLOBAL_KEY] = cfg;
        return;
      }
    } catch {
      // Config file unavailable — keep defaults.
    }
    const host = globalHost();
    if (host) host[GLOBAL_KEY] = DEFAULT_CONFIG;
  }

  get(): AppConfig {
    return globalHost()?.[GLOBAL_KEY] ?? DEFAULT_CONFIG;
  }

  get gatewayUrl(): string {
    return getAppConfigGatewayUrl();
  }
}

/** Synchronous lookup usable from interceptors that may be instantiated in
 * MFE bundles separate from the shell. Returns empty string when not yet
 * loaded or not configured. */
export function getAppConfigGatewayUrl(): string {
  return (globalHost()?.[GLOBAL_KEY]?.gatewayUrl ?? '').replace(/\/+$/, '');
}
