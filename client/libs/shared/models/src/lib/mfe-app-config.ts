import {
  ApplicationConfig,
  isDevMode,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, Routes } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideTransloco } from '@jsverse/transloco';
import { authInterceptor, provideAuth } from '@yumney/shared/auth';
import { TranslocoHttpLoader } from './transloco-loader';

export function createMfeAppConfig(routes: Routes): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideZoneChangeDetection({ eventCoalescing: true }),
      provideRouter(routes),
      provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
      provideAuth(),
      provideTransloco({
        config: {
          availableLangs: ['en', 'de'],
          defaultLang: 'en',
          reRenderOnLangChange: true,
          prodMode: !isDevMode(),
        },
        loader: TranslocoHttpLoader,
      }),
    ],
  };
}
