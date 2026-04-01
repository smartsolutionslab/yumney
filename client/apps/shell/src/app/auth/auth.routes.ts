import { Route } from '@angular/router';
import { guestGuard } from '@yumney/shared/auth';

export const authRoutes: Route[] = [
  {
    path: 'login',
    title: 'Sign In — Yumney',
    canActivate: [guestGuard],
    loadComponent: () => import('./login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    title: 'Register — Yumney',
    canActivate: [guestGuard],
    loadComponent: () => import('./register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'resend-verification',
    title: 'Resend Verification — Yumney',
    loadComponent: () => import('./resend-verification/resend-verification.component').then((m) => m.ResendVerificationComponent),
  },
];
