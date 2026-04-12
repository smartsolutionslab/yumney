import { Route } from '@angular/router';

export const accountRoutes: Route[] = [
  {
    path: '',
    title: 'Profile Settings — Yumney',
    loadComponent: () =>
      import('./profile-settings/profile-settings.component').then(
        (m) => m.ProfileSettingsComponent,
      ),
  },
];
