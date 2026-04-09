import { createMfeAppConfig } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { shoppingRoutes } from './shopping.routes';

export const appConfig = createMfeAppConfig(shoppingRoutes, [provideYumneyIcons()]);
