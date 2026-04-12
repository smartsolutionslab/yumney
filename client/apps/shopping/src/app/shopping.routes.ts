import { Route } from '@angular/router';

export const shoppingRoutes: Route[] = [
  {
    path: 'create/:recipeIdentifier',
    title: 'Create Shopping List — Yumney',
    loadComponent: () =>
      import('./shopping-create/shopping-create.component').then((m) => m.ShoppingCreateComponent),
  },
  {
    path: 'lists/:identifier',
    title: 'Shopping List — Yumney',
    loadComponent: () =>
      import('./shopping-detail/shopping-detail.component').then((m) => m.ShoppingDetailComponent),
  },
  {
    path: 'lists',
    title: 'Shopping Lists — Yumney',
    loadComponent: () =>
      import('./shopping-list/shopping-list.component').then((m) => m.ShoppingListComponent),
  },
  {
    path: '',
    title: 'Shopping — Yumney',
    loadComponent: () =>
      import('./merged-list/merged-list.component').then((m) => m.MergedListComponent),
  },
];
