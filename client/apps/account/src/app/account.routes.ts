import { Route } from '@angular/router';

export const accountRoutes: Route[] = [
  {
    path: '',
    loadComponent: () =>
      import('./account-placeholder/account-placeholder.component').then(
        (m) => m.AccountPlaceholderComponent,
      ),
  },
];
