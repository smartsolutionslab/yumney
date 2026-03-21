import { Route } from '@angular/router';

export const recipesRoutes: Route[] = [
  {
    path: ':identifier/edit',
    title: 'Edit Recipe — Yumney',
    loadComponent: () =>
      import('./recipe-edit/recipe-edit.component').then((m) => m.RecipeEditComponent),
  },
  {
    path: ':identifier',
    title: 'Recipe — Yumney',
    loadComponent: () =>
      import('./recipe-detail/recipe-detail.component').then((m) => m.RecipeDetailComponent),
  },
  {
    path: '',
    title: 'My Recipes — Yumney',
    loadComponent: () =>
      import('./recipe-list/recipe-list.component').then((m) => m.RecipeListComponent),
  },
];
