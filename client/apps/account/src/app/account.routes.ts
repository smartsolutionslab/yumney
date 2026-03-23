import { Route } from '@angular/router';

export const accountRoutes: Route[] = [
  {
    path: '',
    title: 'Account — Yumney',
    loadComponent: () =>
      import('./account-placeholder/account-placeholder.component').then(
        (m) => m.AccountPlaceholderComponent,
      ),
  },
];
