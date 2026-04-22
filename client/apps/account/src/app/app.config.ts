import { createMfeAppConfig } from '@yumney/shared/models';
import { provideYumneyIcons } from '@yumney/ui';
import { accountRoutes } from './account.routes';

export const appConfig = createMfeAppConfig(accountRoutes, {
  scope: 'account',
  extraProviders: [provideYumneyIcons()],
});
