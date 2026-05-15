import {
  ApplicationConfig,
  EnvironmentProviders,
  isDevMode,
  Provider,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideTransloco, provideTranslocoScope } from '@jsverse/transloco';
import { apiBaseInterceptor, authInterceptor, provideAuth } from '@yumney/shared/auth';
import { TranslocoHttpLoader } from './transloco-loader';
import { SUPPORTED_LANGUAGES, DEFAULT_LANGUAGE } from './language-code';
import { globalErrorInterceptor } from './global-error.interceptor';

export interface MfeAppConfigOptions {
  /**
   * Translation scope for this MFE. When provided, Transloco also loads
   * /assets/i18n/{scope}/{lang}.json on top of the main dictionary, and
   * keys under the scope resolve via the alias of the same name (e.g.
   * scope "recipes" registers entries under "recipes.*"). Keeps
   * MFE-owned strings out of the shell's root dictionary when federated.
   */
  scope?: string;
  /** Extra providers appended to the MFE's root injector. */
  extraProviders?: Provider[];
}

export function createMfeAppConfig(routes: Routes, options: MfeAppConfigOptions | Provider[] = {}): ApplicationConfig {
  // Back-compat: accept the old `Provider[]` second arg while call sites migrate.
  const opts: MfeAppConfigOptions = Array.isArray(options) ? { extraProviders: options } : options;

  const providers: (Provider | EnvironmentProviders)[] = [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([apiBaseInterceptor, authInterceptor, globalErrorInterceptor])),
    provideAuth(),
    provideTransloco({
      config: {
        availableLangs: [...SUPPORTED_LANGUAGES],
        defaultLang: DEFAULT_LANGUAGE,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
  ];

  if (opts.scope) {
    providers.push(...provideTranslocoScope(opts.scope));
  }

  if (opts.extraProviders) {
    providers.push(...opts.extraProviders);
  }

  return { providers };
}
