import { createMfeAppConfig } from '@yumney/shared/models';
import { shoppingRoutes } from './shopping.routes';

export const appConfig = createMfeAppConfig(shoppingRoutes);
