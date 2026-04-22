import { DestroyRef, signal } from '@angular/core';
import { of, throwError } from 'rxjs';
import { optimisticSignalUpdate } from './optimistic-update';

interface TestState {
  count: number;
}

const noopDestroyRef: DestroyRef = { onDestroy: () => () => undefined };

describe('optimisticSignalUpdate', () => {
  it('applies the mutation immediately before the API call resolves', () => {
    const value: TestState = { count: 1 };
    const state = signal<TestState | null>(value);
    const apply = vi.fn(() => {
      value.count = 2;
    });
    const rollback = vi.fn();

    optimisticSignalUpdate(state, noopDestroyRef, apply, rollback, () => of(undefined));

    expect(apply).toHaveBeenCalled();
    expect(state()!.count).toBe(2);
  });

  it('does nothing when the state signal holds null', () => {
    const state = signal<TestState | null>(null);
    const apply = vi.fn();
    const rollback = vi.fn();
    const apiCall = vi.fn(() => of(undefined));

    optimisticSignalUpdate(state, noopDestroyRef, apply, rollback, apiCall);

    expect(apply).not.toHaveBeenCalled();
    expect(apiCall).not.toHaveBeenCalled();
  });

  it('rolls back and triggers a signal change when the API errors', () => {
    const value: TestState = { count: 1 };
    const state = signal<TestState | null>(value);
    const apply = vi.fn(() => {
      value.count = 2;
    });
    const rollback = vi.fn(() => {
      value.count = 1;
    });

    optimisticSignalUpdate(state, noopDestroyRef, apply, rollback, () =>
      throwError(() => new Error('boom')),
    );

    expect(rollback).toHaveBeenCalled();
    expect(state()!.count).toBe(1);
  });

  it('does not roll back on success', () => {
    const value: TestState = { count: 1 };
    const state = signal<TestState | null>(value);
    const apply = vi.fn(() => {
      value.count = 2;
    });
    const rollback = vi.fn();

    optimisticSignalUpdate(state, noopDestroyRef, apply, rollback, () => of(undefined));

    expect(rollback).not.toHaveBeenCalled();
    expect(state()!.count).toBe(2);
  });
});
