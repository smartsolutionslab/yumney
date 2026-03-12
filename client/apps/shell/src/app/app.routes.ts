import { Route } from '@angular/router';
import { authGuard } from '@yumney/shared/auth';

export const appRoutes: Route[] = [
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then((m) => m.authRoutes),
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./dashboard/dashboard.component').then((m) => m.DashboardComponent),
  },
  {
    path: 'recipes/:identifier/edit',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./recipes/recipe-edit/recipe-edit.component').then(
        (m) => m.RecipeEditComponent,
      ),
  },
  {
    path: 'recipes/:identifier',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./recipes/recipe-detail/recipe-detail.component').then(
        (m) => m.RecipeDetailComponent,
      ),
  },
  {
    path: 'recipes',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./recipes/recipe-list/recipe-list.component').then((m) => m.RecipeListComponent),
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
];
