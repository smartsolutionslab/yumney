import { Route } from '@angular/router';

export const authRoutes: Route[] = [
  {
    path: 'register',
    loadComponent: () => import('./register/register.component').then((m) => m.RegisterComponent),
  },
];
