import { Route } from '@angular/router';

export const recipesRoutes: Route[] = [
  {
    path: ':identifier/edit',
    loadComponent: () =>
      import('./recipe-edit/recipe-edit.component').then((m) => m.RecipeEditComponent),
  },
  {
    path: ':identifier',
    loadComponent: () =>
      import('./recipe-detail/recipe-detail.component').then((m) => m.RecipeDetailComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./recipe-list/recipe-list.component').then((m) => m.RecipeListComponent),
  },
];
