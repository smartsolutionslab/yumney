import { Route } from '@angular/router';

export const authRoutes: Route[] = [
  {
    path: 'register',
    loadComponent: () => import('./register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'resend-verification',
    loadComponent: () =>
      import('./resend-verification/resend-verification.component').then(
        (m) => m.ResendVerificationComponent,
      ),
  },
];
