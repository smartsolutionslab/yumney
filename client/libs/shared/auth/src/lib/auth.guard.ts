import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { toObservable } from '@angular/core/rxjs-interop';
import { filter, map, take } from 'rxjs';
import { ROUTES } from '@yumney/shared/models';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // If auth has already initialized, check immediately
  if (!authService.isLoading()) {
    return authService.isAuthenticated() ? true : router.createUrlTree([ROUTES.auth.login]);
  }

  // Wait for auth initialization to complete before deciding
  return toObservable(authService.isLoading).pipe(
    filter((loading) => !loading),
    take(1),
    map(() => (authService.isAuthenticated() ? true : router.createUrlTree([ROUTES.auth.login]))),
  );
};
