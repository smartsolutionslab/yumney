import { DestroyRef, WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

interface Favoritable {
  identifier: string;
  isFavorite: boolean;
}

/**
 * Optimistic favorite toggle for a single-item signal (e.g. recipe detail).
 * Flips isFavorite immediately, reverts on error, updates from server on success.
 */
export function toggleFavoriteOnItem<T extends Favoritable>(
  state: WritableSignal<T | null>,
  destroyRef: DestroyRef,
  apiCall: (identifier: string) => Observable<{ isFavorite: boolean }>,
): void {
  const item = state();
  if (!item) return;

  const original = item.isFavorite;
  state.set({ ...item, isFavorite: !original });

  apiCall(item.identifier)
    .pipe(takeUntilDestroyed(destroyRef))
    .subscribe({
      next: (result) => {
        const current = state();
        if (current?.identifier === item.identifier) {
          state.set({ ...current, isFavorite: result.isFavorite });
        }
      },
      error: () => {
        const current = state();
        if (current?.identifier === item.identifier) {
          state.set({ ...current, isFavorite: original });
        }
      },
    });
}

/**
 * Optimistic favorite toggle for a list signal (e.g. recipe list).
 * Flips the item in the array immediately, reverts on error, updates from server on success.
 */
export function toggleFavoriteInList<T extends Favoritable>(
  state: WritableSignal<T[]>,
  identifier: string,
  destroyRef: DestroyRef,
  apiCall: (identifier: string) => Observable<{ isFavorite: boolean }>,
): void {
  const list = state();
  const idx = list.findIndex((r) => r.identifier === identifier);
  if (idx === -1) return;

  const original = list[idx];
  state.update((items) => {
    const next = [...items];
    next[idx] = { ...original, isFavorite: !original.isFavorite };
    return next;
  });

  apiCall(identifier)
    .pipe(takeUntilDestroyed(destroyRef))
    .subscribe({
      next: (result) => {
        state.update((items) => {
          const j = items.findIndex((r) => r.identifier === identifier);
          if (j === -1) return items;
          const next = [...items];
          next[j] = { ...next[j], isFavorite: result.isFavorite };
          return next;
        });
      },
      error: () => {
        state.update((items) => {
          const j = items.findIndex((r) => r.identifier === identifier);
          if (j === -1) return items;
          const next = [...items];
          next[j] = { ...next[j], isFavorite: original.isFavorite };
          return next;
        });
      },
    });
}
