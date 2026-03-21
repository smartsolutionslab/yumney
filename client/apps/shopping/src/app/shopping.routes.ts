import { Route } from '@angular/router';

export const shoppingRoutes: Route[] = [
  {
    path: 'create/:recipeIdentifier',
    title: 'Create Shopping List — Yumney',
    loadComponent: () =>
      import('./shopping-create/shopping-create.component').then(
        (m) => m.ShoppingCreateComponent,
      ),
  },
  {
    path: ':identifier',
    title: 'Shopping List — Yumney',
    loadComponent: () =>
      import('./shopping-detail/shopping-detail.component').then(
        (m) => m.ShoppingDetailComponent,
      ),
  },
  {
    path: '',
    title: 'Shopping Lists — Yumney',
    loadComponent: () =>
      import('./shopping-list/shopping-list.component').then((m) => m.ShoppingListComponent),
  },
];
