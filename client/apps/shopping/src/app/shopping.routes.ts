import { Route } from '@angular/router';

export const shoppingRoutes: Route[] = [
  {
    path: 'create/:recipeIdentifier',
    loadComponent: () =>
      import('./shopping-create/shopping-create.component').then(
        (m) => m.ShoppingCreateComponent,
      ),
  },
  {
    path: ':identifier',
    loadComponent: () =>
      import('./shopping-detail/shopping-detail.component').then(
        (m) => m.ShoppingDetailComponent,
      ),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shopping-list/shopping-list.component').then((m) => m.ShoppingListComponent),
  },
];
