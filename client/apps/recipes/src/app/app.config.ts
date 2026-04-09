import { createMfeAppConfig } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { recipesRoutes } from './recipes.routes';

export const appConfig = createMfeAppConfig(recipesRoutes, [provideYumneyIcons()]);
