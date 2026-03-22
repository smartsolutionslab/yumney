import { createMfeAppConfig } from '@yumney/shared/models';
import { accountRoutes } from './account.routes';

export const appConfig = createMfeAppConfig(accountRoutes);
