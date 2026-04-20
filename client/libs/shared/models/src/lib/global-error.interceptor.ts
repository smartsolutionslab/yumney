import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from './toast.service';

// Catches errors that per-component handlers can't meaningfully recover from
// (network offline, CORS, upstream unavailability) and surfaces a single toast.
// The error is always rethrown so component-level handlers still run.
export const globalErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toasts = inject(ToastService);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        if (err.status === 0) {
          toasts.error('common.errors.networkUnavailable');
        } else if (err.status === 503) {
          toasts.error('common.errors.serviceUnavailable');
        }
      }
      return throwError(() => err);
    }),
  );
};
