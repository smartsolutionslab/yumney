import { Route } from '@angular/router';
import { guestGuard } from '@yumney/shared/auth';

export const authRoutes: Route[] = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
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
