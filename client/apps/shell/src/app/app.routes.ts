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
      import('./recipes/recipe-edit/recipe-edit.component').then((m) => m.RecipeEditComponent),
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
    path: 'shopping/create/:recipeIdentifier',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shopping/shopping-create/shopping-create.component').then(
        (m) => m.ShoppingCreateComponent,
      ),
  },
  {
    path: 'shopping/:identifier',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shopping/shopping-detail/shopping-detail.component').then(
        (m) => m.ShoppingDetailComponent,
      ),
  },
  {
    path: 'shopping',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shopping/shopping-list/shopping-list.component').then(
        (m) => m.ShoppingListComponent,
      ),
  },
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
];
