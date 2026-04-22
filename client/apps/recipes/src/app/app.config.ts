import { createMfeAppConfig } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { recipesRoutes } from './recipes.routes';

export const appConfig = createMfeAppConfig(recipesRoutes, {
  scope: 'recipes',
  extraProviders: [provideYumneyIcons()],
});
