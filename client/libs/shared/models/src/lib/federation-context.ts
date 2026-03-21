import { InjectionToken } from '@angular/core';

/**
 * Determines if this app is running standalone or as a federated remote.
 *
 * Detection: When the shell bootstraps, it loads MFE routes via `loadRemoteModule()`.
 * The MFE code runs inside the shell's Angular platform. We detect this by checking
 * if our bootstrap entry point's host element is the document root or nested.
 *
 * Simpler heuristic: if the current URL port matches our own serve port, we're standalone.
 * In production, the shell and MFEs are served from the same origin, so we check if
 * the shell's root element `yn-root` exists in the DOM (shell bootstraps first).
 */
export const IS_STANDALONE = new InjectionToken<boolean>('IS_STANDALONE', {
  providedIn: 'root',
  factory: () => {
    try {
      // If the shell's root component is in the DOM, we're running inside the shell
      return !document.querySelector('yn-root');
    } catch {
      return true;
    }
  },
});
