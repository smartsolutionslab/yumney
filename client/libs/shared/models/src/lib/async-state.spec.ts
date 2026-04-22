import { DestroyRef } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { createAsyncState } from './async-state';

const noopDestroyRef: DestroyRef = { onDestroy: () => () => undefined };

describe('createAsyncState', () => {
  it('flips isLoading while an observable is in flight', () => {
    const state = createAsyncState(noopDestroyRef);
    const onSuccess = vi.fn();

    state.execute(of('done'), { default: 'err' }, onSuccess);

    // sync emission => isLoading settles back to false
    expect(state.isLoading()).toBe(false);
    expect(onSuccess).toHaveBeenCalledWith('done');
    expect(state.serverError()).toBeNull();
  });

  it('maps HTTP errors to keys via the error map and writes to serverError', () => {
    const state = createAsyncState(noopDestroyRef);
    const onSuccess = vi.fn();
    const err = new HttpErrorResponse({ status: 404 });

    state.execute(
      throwError(() => err),
      { 404: 'not-found', default: 'generic' },
      onSuccess,
    );

    expect(state.isLoading()).toBe(false);
    expect(state.serverError()).toBe('not-found');
    expect(onSuccess).not.toHaveBeenCalled();
  });

  it('falls back to the default map entry when the status is not listed', () => {
    const state = createAsyncState(noopDestroyRef);
    const err = new HttpErrorResponse({ status: 500 });

    state.execute(
      throwError(() => err),
      { 404: 'not-found', default: 'generic' },
      vi.fn(),
    );

    expect(state.serverError()).toBe('generic');
  });

  it('routes errors to the onError callback when provided and does not set serverError', () => {
    const state = createAsyncState(noopDestroyRef);
    const onSuccess = vi.fn();
    const onError = vi.fn();
    const err = new HttpErrorResponse({ status: 404 });

    state.execute(
      throwError(() => err),
      { 404: 'not-found', default: 'generic' },
      onSuccess,
      onError,
    );

    expect(onError).toHaveBeenCalledWith('not-found');
    expect(state.serverError()).toBeNull();
  });

  it('clears serverError before a new request only when no onError handler is used', () => {
    const state = createAsyncState(noopDestroyRef);
    state.execute(
      throwError(() => new HttpErrorResponse({ status: 500 })),
      { default: 'x' },
      vi.fn(),
    );
    expect(state.serverError()).toBe('x');

    state.execute(of('ok'), { default: 'x' }, vi.fn());

    expect(state.serverError()).toBeNull();
  });
});
