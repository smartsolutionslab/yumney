import { createMfeAppConfig } from '@yumney/shared/models';
import { recipesRoutes } from './recipes.routes';

export const appConfig = createMfeAppConfig(recipesRoutes);
