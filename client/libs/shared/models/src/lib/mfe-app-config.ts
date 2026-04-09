import {
  ApplicationConfig,
  isDevMode,
  Provider,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideTransloco } from '@jsverse/transloco';
import { authInterceptor, provideAuth } from '@yumney/shared/auth';
import { TranslocoHttpLoader } from './transloco-loader';
import { SUPPORTED_LANGUAGES, DEFAULT_LANGUAGE } from './language-code';

export function createMfeAppConfig(routes: Routes, extra: Provider[] = []): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideZoneChangeDetection({ eventCoalescing: true }),
      provideRouter(routes),
      provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
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
      ...extra,
    ],
  };
}
