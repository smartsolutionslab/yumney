import { Route } from '@angular/router';
import { loadRemoteModule } from '@angular-architects/native-federation';
import { authGuard } from '@yumney/shared/auth';

export const appRoutes: Route[] = [
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then((m) => m.authRoutes),
  },
  {
    path: 'dashboard',
    title: 'Dashboard — Yumney',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./dashboard/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'recipes',
    title: 'Recipes — Yumney',
    canActivate: [authGuard],
    loadChildren: () =>
      loadRemoteModule('recipes', './routes').then((m) => m.recipesRoutes),
  },
  {
    path: 'shopping',
    title: 'Shopping Lists — Yumney',
    canActivate: [authGuard],
    loadChildren: () =>
      loadRemoteModule('shopping', './routes').then((m) => m.shoppingRoutes),
  },
  {
    path: 'account',
    title: 'Account — Yumney',
    canActivate: [authGuard],
    loadChildren: () =>
      loadRemoteModule('account', './routes').then((m) => m.accountRoutes),
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
];
