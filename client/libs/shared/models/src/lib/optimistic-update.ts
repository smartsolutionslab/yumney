import { DestroyRef, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

/**
 * Applies an optimistic mutation to a signal-held object, fires the API call,
 * and rolls back the mutation on error. Triggers signal change detection by
 * shallow-cloning the value after both apply and rollback.
 */
export function optimisticSignalUpdate<T extends object>(
  state: WritableSignal<T | null>,
  destroyRef: DestroyRef,
  apply: () => void,
  rollback: () => void,
  apiCall: (value: T) => Observable<unknown>,
): void {
  const value = state();
  if (!value) return;

  apply();
  state.set({ ...value });

  apiCall(value)
    .pipe(takeUntilDestroyed(destroyRef))
    .subscribe({
      error: () => {
        rollback();
        state.set({ ...value });
      },
    });
}
