import { DestroyRef } from '@angular/core';
import { attachHorizontalSwipe } from './attach-horizontal-swipe';

function makeDestroyRef(): { ref: DestroyRef; destroy: () => void } {
  const callbacks: Array<() => void> = [];
  return {
    ref: { destroyed: false, onDestroy: (cb: () => void) => callbacks.push(cb) },
    destroy: () => callbacks.forEach((cb) => cb()),
  };
}

describe('attachHorizontalSwipe', () => {
  let host: HTMLElement;
  let onSwipeLeft: ReturnType<typeof vi.fn>;
  let onSwipeRight: ReturnType<typeof vi.fn>;
  let destroyRef: ReturnType<typeof makeDestroyRef>;

  beforeEach(() => {
    host = document.createElement('div');
    document.body.appendChild(host);
    onSwipeLeft = vi.fn();
    onSwipeRight = vi.fn();
    destroyRef = makeDestroyRef();
    attachHorizontalSwipe(host, destroyRef.ref, { onSwipeLeft, onSwipeRight });
  });

  afterEach(() => {
    document.body.removeChild(host);
  });

  function fire(type: 'pointerdown' | 'pointerup', x: number, y: number, pointerType: 'touch' | 'mouse' = 'touch'): void {
    host.dispatchEvent(new PointerEvent(type, { pointerType, clientX: x, clientY: y }));
  }

  it('fires onSwipeLeft for a sufficient leftward touch drag', () => {
    fire('pointerdown', 300, 100);
    fire('pointerup', 200, 105);
    expect(onSwipeLeft).toHaveBeenCalled();
    expect(onSwipeRight).not.toHaveBeenCalled();
  });

  it('fires onSwipeRight for a sufficient rightward touch drag', () => {
    fire('pointerdown', 100, 100);
    fire('pointerup', 220, 95);
    expect(onSwipeRight).toHaveBeenCalled();
    expect(onSwipeLeft).not.toHaveBeenCalled();
  });

  it('ignores mouse drags', () => {
    fire('pointerdown', 300, 100, 'mouse');
    fire('pointerup', 150, 100, 'mouse');
    expect(onSwipeLeft).not.toHaveBeenCalled();
    expect(onSwipeRight).not.toHaveBeenCalled();
  });

  it('ignores drags shorter than the threshold', () => {
    fire('pointerdown', 300, 100);
    fire('pointerup', 260, 105);
    expect(onSwipeLeft).not.toHaveBeenCalled();
    expect(onSwipeRight).not.toHaveBeenCalled();
  });

  it('ignores mostly-vertical drags (treat as scroll, not swipe)', () => {
    fire('pointerdown', 200, 100);
    fire('pointerup', 260, 300);
    expect(onSwipeLeft).not.toHaveBeenCalled();
    expect(onSwipeRight).not.toHaveBeenCalled();
  });

  it('removes listeners when the destroyRef fires', () => {
    destroyRef.destroy();
    fire('pointerdown', 300, 100);
    fire('pointerup', 200, 105);
    expect(onSwipeLeft).not.toHaveBeenCalled();
  });
});
