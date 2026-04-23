import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

console.log('[shell] bootstrap: calling bootstrapApplication');
(globalThis as Record<string, unknown>)['__ynBootstrapCalled'] = Date.now();

bootstrapApplication(App, appConfig)
  .then(() => {
    console.log('[shell] bootstrap: bootstrapApplication resolved');
    (globalThis as Record<string, unknown>)['__ynBootstrapDone'] = Date.now();
  })
  .catch((err) => console.error('[shell] bootstrap: bootstrapApplication rejected', err));
