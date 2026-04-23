import { initFederation } from '@angular-architects/native-federation';

// Ensure ngDevMode is defined before Angular's signal() is used in federated modules.
// Native Federation loads bundles where this global may not yet exist.
(globalThis as Record<string, unknown>)['ngDevMode'] ??= false;

// Diagnostic markers so E2E can tell where bootstrap stalls.
(globalThis as Record<string, unknown>)['__ynMainStart'] = Date.now();
console.log('[shell] main: initFederation start');

initFederation('/assets/federation.manifest.json')
  .then(() => {
    console.log('[shell] main: initFederation resolved');
    (globalThis as Record<string, unknown>)['__ynFederationReady'] = Date.now();
  })
  .catch((err) => {
    console.error('[shell] main: initFederation rejected', err);
    throw err;
  })
  .then(() => {
    console.log('[shell] main: importing bootstrap');
    return import('./bootstrap');
  })
  .then(() => {
    console.log('[shell] main: bootstrap import resolved');
    (globalThis as Record<string, unknown>)['__ynBootstrapImported'] = Date.now();
  })
  .catch((err) => console.error('[shell] main: bootstrap import failed', err));
