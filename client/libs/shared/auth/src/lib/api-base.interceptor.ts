import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { AppConfigService } from './app-config.service';

/**
 * Rewrites relative `/api/*` URLs to absolute URLs on the Gateway so browser
 * fetches from the Angular dev server (which has no /api proxy) reach the
 * YARP gateway directly. In production the frontend is served through the
 * gateway, so gatewayUrl is empty and URLs stay relative.
 *
 * Must run BEFORE authInterceptor so the Authorization header is added after
 * the final URL is resolved.
 */
export const apiBaseInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/') && !req.url.startsWith('/realms/')) {
    return next(req);
  }

  const config = inject(AppConfigService);
  const base = config.gatewayUrl;
  if (!base) return next(req);

  return next(req.clone({ url: `${base}${req.url}` }));
};
