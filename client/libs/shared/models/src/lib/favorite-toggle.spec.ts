import { DestroyRef, signal } from '@angular/core';
import { of, Subject, throwError } from 'rxjs';
import { toggleFavoriteInList, toggleFavoriteOnItem } from './favorite-toggle';

const noopDestroyRef: DestroyRef = { onDestroy: () => () => undefined };

interface Item {
  identifier: string;
  isFavorite: boolean;
  title?: string;
}

describe('toggleFavoriteOnItem', () => {
  it('flips isFavorite immediately (optimistic)', () => {
    const state = signal<Item | null>({ identifier: 'a', isFavorite: false });
    const api = vi.fn(() => of({ isFavorite: true }));

    toggleFavoriteOnItem(state, noopDestroyRef, api);

    expect(state()!.isFavorite).toBe(true);
    expect(api).toHaveBeenCalledWith('a');
  });

  it('overwrites with the server value on success', () => {
    const state = signal<Item | null>({ identifier: 'a', isFavorite: false });
    const api = vi.fn(() => of({ isFavorite: true }));

    toggleFavoriteOnItem(state, noopDestroyRef, api);

    expect(state()!.isFavorite).toBe(true);
  });

  it('rolls back the optimistic flip on error', () => {
    const state = signal<Item | null>({ identifier: 'a', isFavorite: true });
    const api = vi.fn(() => throwError(() => new Error('boom')));

    toggleFavoriteOnItem(state, noopDestroyRef, api);

    expect(state()!.isFavorite).toBe(true);
  });

  it('does nothing when the signal holds null', () => {
    const state = signal<Item | null>(null);
    const api = vi.fn(() => of({ isFavorite: true }));

    toggleFavoriteOnItem(state, noopDestroyRef, api);

    expect(api).not.toHaveBeenCalled();
  });

  it('ignores the response if the item in the signal has been replaced', () => {
    const state = signal<Item | null>({ identifier: 'a', isFavorite: false });
    const pending = new Subject<{ isFavorite: boolean }>();
    toggleFavoriteOnItem(state, noopDestroyRef, () => pending.asObservable());

    // User navigates to a different recipe before the response arrives.
    state.set({ identifier: 'b', isFavorite: false });
    pending.next({ isFavorite: true });

    expect(state()!.identifier).toBe('b');
    expect(state()!.isFavorite).toBe(false);
  });
});

describe('toggleFavoriteInList', () => {
  it('flips the matching item immediately', () => {
    const state = signal<Item[]>([
      { identifier: 'a', isFavorite: false },
      { identifier: 'b', isFavorite: false },
    ]);
    const api = vi.fn(() => of({ isFavorite: true }));

    toggleFavoriteInList(state, 'a', noopDestroyRef, api);

    expect(state()[0].isFavorite).toBe(true);
    expect(state()[1].isFavorite).toBe(false);
  });

  it('does nothing when the identifier is not in the list', () => {
    const state = signal<Item[]>([{ identifier: 'a', isFavorite: false }]);
    const api = vi.fn(() => of({ isFavorite: true }));

    toggleFavoriteInList(state, 'missing', noopDestroyRef, api);

    expect(api).not.toHaveBeenCalled();
    expect(state()[0].isFavorite).toBe(false);
  });

  it('rolls the item back to its original state on error', () => {
    const state = signal<Item[]>([{ identifier: 'a', isFavorite: false }]);
    const api = vi.fn(() => throwError(() => new Error('boom')));

    toggleFavoriteInList(state, 'a', noopDestroyRef, api);

    expect(state()[0].isFavorite).toBe(false);
  });

  it('preserves the rest of the list when rolling back', () => {
    const state = signal<Item[]>([
      { identifier: 'a', isFavorite: false },
      { identifier: 'b', isFavorite: true, title: 'other' },
    ]);

    toggleFavoriteInList(state, 'a', noopDestroyRef, () => throwError(() => new Error('x')));

    expect(state()[1]).toEqual({ identifier: 'b', isFavorite: true, title: 'other' });
  });
});
