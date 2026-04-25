import { Route } from '@angular/router';
import { provideTranslocoScope } from '@jsverse/transloco';

// Native federation only loads the routes export, not the MFE's app.config.
// Register the Transloco scope at the route level so recipes.* keys resolve
// whether the app runs standalone or as a federated remote (mirrors the
// account fix in #343 / account.routes.ts).
const RECIPES_SCOPE_PROVIDERS = [provideTranslocoScope('recipes')];

export const recipesRoutes: Route[] = [
  {
    path: ':identifier/edit',
    title: 'Edit Recipe — Yumney',
    providers: RECIPES_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./recipe-edit/recipe-edit.component').then((m) => m.RecipeEditComponent),
  },
  {
    path: ':identifier/cook',
    title: 'Cook Mode — Yumney',
    providers: RECIPES_SCOPE_PROVIDERS,
    loadComponent: () => import('./cook-mode/cook-mode.component').then((m) => m.CookModeComponent),
  },
  {
    path: ':identifier',
    title: 'Recipe — Yumney',
    providers: RECIPES_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./recipe-detail/recipe-detail.component').then((m) => m.RecipeDetailComponent),
  },
  {
    path: '',
    title: 'My Recipes — Yumney',
    providers: RECIPES_SCOPE_PROVIDERS,
    loadComponent: () =>
      import('./recipe-list/recipe-list.component').then((m) => m.RecipeListComponent),
  },
];
