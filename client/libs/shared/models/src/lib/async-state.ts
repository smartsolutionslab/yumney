import { signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { mapHttpError, HttpErrorMap } from './http-error-utils';

export function createAsyncState(destroyRef: DestroyRef) {
  const isLoading = signal(false);
  const serverError = signal<string | null>(null);

  function execute<T>(
    source: Observable<T>,
    errorMap: HttpErrorMap,
    onSuccess: (result: T) => void,
    onError?: (error: string) => void,
  ): void {
    isLoading.set(true);
    if (!onError) {
      serverError.set(null);
    }

    source.pipe(takeUntilDestroyed(destroyRef)).subscribe({
      next: (result) => {
        isLoading.set(false);
        onSuccess(result);
      },
      error: (err: HttpErrorResponse) => {
        isLoading.set(false);
        const mapped = mapHttpError(err, errorMap);
        if (onError) {
          onError(mapped);
        } else {
          serverError.set(mapped);
        }
      },
    });
  }

  return { isLoading, serverError, execute };
}

export type AsyncState = ReturnType<typeof createAsyncState>;
