import { Route } from '@angular/router';
import { provideTranslocoScope } from '@jsverse/transloco';

// Native federation only loads the routes export, not the MFE's app.config.
// Register the Transloco scope at the route level so shopping.* keys
// resolve whether the app runs standalone or as a federated remote
// (mirrors the account fix in #343 / account.routes.ts).
const SHOPPING_SCOPE_PROVIDERS = [provideTranslocoScope('shopping')];

export const shoppingRoutes: Route[] = [
  {
    path: 'create/:recipeIdentifier',
    title: 'Create Shopping List — Yumney',
    providers: SHOPPING_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./shopping-create/shopping-create.component').then((m) => m.ShoppingCreateComponent),
  },
  {
    path: 'lists/:identifier',
    title: 'Shopping List — Yumney',
    providers: SHOPPING_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./shopping-detail/shopping-detail.component').then((m) => m.ShoppingDetailComponent),
  },
  {
    path: 'lists',
    title: 'Shopping Lists — Yumney',
    providers: SHOPPING_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./shopping-list/shopping-list.component').then((m) => m.ShoppingListComponent),
  },
  {
    path: '',
    title: 'Shopping — Yumney',
    providers: SHOPPING_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./merged-list/merged-list.component').then((m) => m.MergedListComponent),
  },
];
