import { HttpInterceptorFn } from '@angular/common/http';
import { getAppConfigGatewayUrl } from './app-config.service';

/**
 * Rewrites relative `/api/*` URLs to absolute URLs on the Gateway so browser
 * fetches from the Angular dev server (which has no /api proxy) reach the
 * YARP gateway directly. In production the frontend is served through the
 * gateway, so gatewayUrl is empty and URLs stay relative.
 *
 * Reads gatewayUrl via a module-level global (see app-config.service.ts)
 * rather than injection — native federation bundles workspace libs per-MFE,
 * so `inject(AppConfigService)` in an MFE resolves to a different class
 * token than the one the shell populated, and DI lookup returns an empty
 * instance. The global is federation-safe.
 *
 * Order matters: register this before authInterceptor so the Authorization
 * header lands on the rewritten absolute URL.
 */
export const apiBaseInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/') && !req.url.startsWith('/realms/')) {
    return next(req);
  }

  const base = getAppConfigGatewayUrl();
  if (!base) return next(req);

  return next(req.clone({ url: `${base}${req.url}` }));
};
