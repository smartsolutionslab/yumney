import { TestBed } from '@angular/core/testing';
import { WakeLockService } from './wake-lock.service';

interface FakeSentinel {
  released: boolean;
  release: ReturnType<typeof vi.fn>;
  addEventListener: ReturnType<typeof vi.fn>;
  removeEventListener: ReturnType<typeof vi.fn>;
  triggerRelease(): void;
}

function createFakeSentinel(): FakeSentinel {
  let releaseHandler: (() => void) | null = null;
  return {
    released: false,
    release: vi.fn().mockResolvedValue(undefined),
    addEventListener: vi.fn((_type: string, handler: () => void) => {
      releaseHandler = handler;
    }),
    removeEventListener: vi.fn(),
    triggerRelease() {
      releaseHandler?.();
    },
  };
}

describe('WakeLockService', () => {
  let service: WakeLockService;
  let originalWakeLock: unknown;

  beforeEach(() => {
    originalWakeLock = (navigator as unknown as { wakeLock?: unknown }).wakeLock;
  });

  afterEach(() => {
    Object.defineProperty(navigator, 'wakeLock', {
      configurable: true,
      value: originalWakeLock,
    });
  });

  it('should report unsupported when wakeLock is missing', () => {
    Object.defineProperty(navigator, 'wakeLock', { configurable: true, value: undefined });
    service = TestBed.runInInjectionContext(() => new WakeLockService());

    expect(service.supported()).toBe(false);
  });

  it('should acquire and set active when supported', async () => {
    const sentinel = createFakeSentinel();
    Object.defineProperty(navigator, 'wakeLock', {
      configurable: true,
      value: { request: vi.fn().mockResolvedValue(sentinel) },
    });
    service = TestBed.runInInjectionContext(() => new WakeLockService());

    await service.acquire();

    expect(service.active()).toBe(true);
  });

  it('should release sentinel and clear active state', async () => {
    const sentinel = createFakeSentinel();
    Object.defineProperty(navigator, 'wakeLock', {
      configurable: true,
      value: { request: vi.fn().mockResolvedValue(sentinel) },
    });
    service = TestBed.runInInjectionContext(() => new WakeLockService());

    await service.acquire();
    await service.release();

    expect(sentinel.release).toHaveBeenCalled();
    expect(service.active()).toBe(false);
  });

  it('should clear active when sentinel emits release event', async () => {
    const sentinel = createFakeSentinel();
    Object.defineProperty(navigator, 'wakeLock', {
      configurable: true,
      value: { request: vi.fn().mockResolvedValue(sentinel) },
    });
    service = TestBed.runInInjectionContext(() => new WakeLockService());

    await service.acquire();
    sentinel.triggerRelease();

    expect(service.active()).toBe(false);
  });

  it('should swallow errors during acquire', async () => {
    Object.defineProperty(navigator, 'wakeLock', {
      configurable: true,
      value: { request: vi.fn().mockRejectedValue(new Error('blocked')) },
    });
    service = TestBed.runInInjectionContext(() => new WakeLockService());

    await service.acquire();

    expect(service.active()).toBe(false);
  });
});
