import { Route } from '@angular/router';
import { guestGuard } from '@yumney/shared/auth';

export const authRoutes: Route[] = [
  {
    path: 'login',
    title: 'Sign In — Yumney',
    canActivate: [guestGuard],
    loadComponent: () => import('./login').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    title: 'Register — Yumney',
    canActivate: [guestGuard],
    loadComponent: () => import('./register').then((m) => m.RegisterComponent),
  },
  {
    path: 'resend-verification',
    title: 'Resend Verification — Yumney',
    loadComponent: () => import('./resend-verification').then((m) => m.ResendVerificationComponent),
  },
];
