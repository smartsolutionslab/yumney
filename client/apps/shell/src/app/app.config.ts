import {
  ApplicationConfig,
  isDevMode,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
  APP_INITIALIZER,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideServiceWorker } from '@angular/service-worker';
import { provideTransloco } from '@jsverse/transloco';
import { apiBaseInterceptor, authInterceptor, provideAuth } from '@yumney/shared/auth';
import { provideYumneyIcons } from '@yumney/ui';
import { appRoutes } from './app.routes';
import {
  TranslocoHttpLoader,
  LanguageService,
  ThemeService,
  UI,
  SUPPORTED_LANGUAGES,
  DEFAULT_LANGUAGE,
  globalErrorInterceptor,
} from '@yumney/shared/models';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideYumneyIcons(),
    provideHttpClient(
      withFetch(),
      withInterceptors([apiBaseInterceptor, authInterceptor, globalErrorInterceptor]),
    ),
    provideAuth(),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: `registerWhenStable:${UI.SERVICE_WORKER_REGISTRATION_MS}`,
    }),
    provideTransloco({
      config: {
        availableLangs: [...SUPPORTED_LANGUAGES],
        defaultLang: DEFAULT_LANGUAGE,
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
    {
      provide: APP_INITIALIZER,
      useFactory: (lang: LanguageService, theme: ThemeService) => () =>
        Promise.all([lang.initialize(), theme.initialize()]),
      deps: [LanguageService, ThemeService],
      multi: true,
    },
  ],
};
