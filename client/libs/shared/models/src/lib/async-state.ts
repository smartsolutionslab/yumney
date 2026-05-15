import { signal, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { mapHttpError, HttpErrorMap } from './http-error-utils';

/**
 * Creates a single async-state slot. Pass an explicit `DestroyRef` from
 * outside an injection context (tests, services holding state for someone
 * else); call without args inside a component / service constructor and
 * the current injection context's `DestroyRef` is resolved automatically.
 */
export function createAsyncState(destroyRef?: DestroyRef): AsyncState {
  const teardown = destroyRef ?? inject(DestroyRef);
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

    source.pipe(takeUntilDestroyed(teardown)).subscribe({
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

export interface AsyncState {
  isLoading: ReturnType<typeof signal<boolean>>;
  serverError: ReturnType<typeof signal<string | null>>;
  execute<T>(source: Observable<T>, errorMap: HttpErrorMap, onSuccess: (result: T) => void, onError?: (error: string) => void): void;
}

/**
 * Variadic factory for components that juggle multiple concurrent async
 * operations (e.g. a detail page with `load`, `delete`, `save`). Each key
 * yields an independent `AsyncState`. Must run in an injection context;
 * the shared `DestroyRef` is resolved internally and reused for every slot.
 */
export function injectAsyncStates<const Keys extends readonly string[]>(...keys: Keys): Record<Keys[number], AsyncState> {
  const destroyRef = inject(DestroyRef);
  const result = {} as Record<Keys[number], AsyncState>;
  for (const key of keys) {
    result[key as Keys[number]] = createAsyncState(destroyRef);
  }
  return result;
}
