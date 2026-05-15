import { Signal, effect } from '@angular/core';

/**
 * Runs `callback` after `ms` of quiet whenever `source` changes. Each new
 * change resets the timer; the latest value wins. The first run (the value
 * present at registration time) is skipped, matching `rxjs.skip(1)`.
 *
 * Must be called from an injection context — the underlying `effect` ties
 * its lifetime to the surrounding injector. Pending timeouts are cleared
 * on each re-run and on teardown.
 */
export function debouncedEffect<T>(source: Signal<T>, ms: number, callback: (value: T) => void): void {
  let firstRun = true;
  effect((onCleanup) => {
    const value = source();
    if (firstRun) {
      firstRun = false;
      return;
    }
    const timeoutId = setTimeout(() => callback(value), ms);
    onCleanup(() => clearTimeout(timeoutId));
  });
}
