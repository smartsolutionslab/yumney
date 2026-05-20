import { DestroyRef } from '@angular/core';

const SWIPE_MIN_DX_PX = 60;
// |dy| must be < 50% of |dx| to count as horizontal — anything more vertical
// is treated as a scroll gesture, not a swipe.
const SWIPE_MAX_DY_RATIO = 0.5;

export interface HorizontalSwipeCallbacks {
  onSwipeLeft: () => void;
  onSwipeRight: () => void;
}

/**
 * Attach touch-only horizontal swipe handling to a host element. The listeners
 * are removed when `destroyRef` fires.
 *
 * Touch-only by design: mouse drags would conflict with scroll-bar dragging
 * and text selection on desktop.
 */
export function attachHorizontalSwipe(host: HTMLElement, destroyRef: DestroyRef, callbacks: HorizontalSwipeCallbacks): void {
  let start: { x: number; y: number } | null = null;

  const onDown = (event: PointerEvent): void => {
    if (event.pointerType !== 'touch') return;
    start = { x: event.clientX, y: event.clientY };
  };

  const onUp = (event: PointerEvent): void => {
    const begin = start;
    start = null;
    if (!begin || event.pointerType !== 'touch') return;

    const dx = event.clientX - begin.x;
    const dy = event.clientY - begin.y;
    if (Math.abs(dx) < SWIPE_MIN_DX_PX) return;
    if (Math.abs(dy) > Math.abs(dx) * SWIPE_MAX_DY_RATIO) return;

    if (dx < 0) callbacks.onSwipeLeft();
    else callbacks.onSwipeRight();
  };

  host.addEventListener('pointerdown', onDown);
  host.addEventListener('pointerup', onUp);
  destroyRef.onDestroy(() => {
    host.removeEventListener('pointerdown', onDown);
    host.removeEventListener('pointerup', onUp);
  });
}
