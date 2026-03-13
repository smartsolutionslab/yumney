import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { OAuthService } from 'angular-oauth2-oidc';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const oauthService = inject(OAuthService);

  if (req.url.includes('/api/') && oauthService.hasValidAccessToken()) {
    const cloned = req.clone({
      setHeaders: {
        Authorization: `Bearer ${oauthService.getAccessToken()}`,
      },
    });
    return next(cloned);
  }

  return next(req);
};
