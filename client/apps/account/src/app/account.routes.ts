import { Route } from '@angular/router';
import { provideTranslocoScope } from '@jsverse/transloco';

export const accountRoutes: Route[] = [
  {
    path: '',
    title: 'Profile Settings — Yumney',
    // Register the Transloco scope at the route level so it applies whether
    // the app runs standalone (own app.config) or is loaded by the shell via
    // native federation — federation only pulls in the routes, not the MFE's
    // root providers. Without this, keys like account.settings.title render
    // as their literal path.
    providers: [provideTranslocoScope('account')],
    loadComponent: () =>
      import('./profile-settings/profile-settings.component').then(
        (m) => m.ProfileSettingsComponent,
      ),
  },
];
