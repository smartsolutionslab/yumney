import { initFederation } from '@angular-architects/native-federation';

// Ensure ngDevMode is defined before Angular's signal() is used in federated modules.
// Native Federation loads bundles where this global may not yet exist.
(globalThis as Record<string, unknown>)['ngDevMode'] ??= false;

initFederation('/assets/federation.manifest.json')
  .catch((err) => console.error(err))
  .then(() => import('./bootstrap'))
  .catch((err) => console.error(err));
