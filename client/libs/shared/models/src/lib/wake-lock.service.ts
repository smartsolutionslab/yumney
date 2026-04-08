import { Injectable, signal } from '@angular/core';

interface WakeLockSentinelLike {
  released: boolean;
  release(): Promise<void>;
  addEventListener(type: 'release', listener: () => void): void;
}

interface WakeLockNavigator {
  wakeLock?: { request(type: 'screen'): Promise<WakeLockSentinelLike> };
}

@Injectable({ providedIn: 'root' })
export class WakeLockService {
  readonly supported = signal(this.detectSupport());
  readonly active = signal(false);

  private sentinel: WakeLockSentinelLike | null = null;
  private visibilityHandler: (() => void) | null = null;

  async acquire(): Promise<void> {
    if (!this.supported() || this.sentinel !== null) {
      return;
    }
    try {
      const nav = navigator as unknown as WakeLockNavigator;
      const sentinel = await nav.wakeLock!.request('screen');
      this.sentinel = sentinel;
      this.active.set(true);
      sentinel.addEventListener('release', () => {
        this.active.set(false);
        this.sentinel = null;
      });
      this.installVisibilityHandler();
    } catch {
      this.active.set(false);
    }
  }

  async release(): Promise<void> {
    if (this.sentinel) {
      try {
        await this.sentinel.release();
      } catch {
        /* ignore */
      }
      this.sentinel = null;
    }
    this.active.set(false);
    this.removeVisibilityHandler();
  }

  private installVisibilityHandler(): void {
    if (this.visibilityHandler || typeof document === 'undefined') return;
    this.visibilityHandler = () => {
      if (document.visibilityState === 'visible' && this.sentinel === null && this.active()) {
        void this.acquire();
      }
    };
    document.addEventListener('visibilitychange', this.visibilityHandler);
  }

  private removeVisibilityHandler(): void {
    if (this.visibilityHandler && typeof document !== 'undefined') {
      document.removeEventListener('visibilitychange', this.visibilityHandler);
      this.visibilityHandler = null;
    }
  }

  private detectSupport(): boolean {
    if (typeof navigator === 'undefined') return false;
    const nav = navigator as unknown as WakeLockNavigator;
    return nav.wakeLock != null;
  }
}
