import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { ROUTES } from '@yumney/shared/models';
import { AuthService } from './auth.service';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return router.createUrlTree([ROUTES.dashboard]);
  }

  return true;
};
