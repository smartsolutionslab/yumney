import { TestBed } from '@angular/core/testing';
import { UI } from '@yumney/shared/models';
import { OfflineStatusService } from './offline-status.service';

describe('OfflineStatusService', () => {
  let originalDescriptor: PropertyDescriptor | undefined;
  let onLineValue: boolean;

  const setOnline = (value: boolean) => {
    onLineValue = value;
  };

  beforeEach(() => {
    vi.useFakeTimers();
    originalDescriptor = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(navigator), 'onLine');
    onLineValue = true;
    Object.defineProperty(navigator, 'onLine', {
      configurable: true,
      get: () => onLineValue,
    });
  });

  afterEach(() => {
    if (originalDescriptor) {
      Object.defineProperty(Object.getPrototypeOf(navigator), 'onLine', originalDescriptor);
    }
    vi.useRealTimers();
  });

  const createService = () => {
    TestBed.configureTestingModule({ providers: [OfflineStatusService] });
    return TestBed.inject(OfflineStatusService);
  };

  it('should initialize isOffline from navigator.onLine', () => {
    setOnline(false);

    const service = createService();

    expect(service.isOffline()).toBe(true);
  });

  it('should be online by default when navigator reports online', () => {
    const service = createService();

    expect(service.isOffline()).toBe(false);
  });

  it('should set isOffline true when offline event fires', () => {
    const service = createService();

    window.dispatchEvent(new Event('offline'));

    expect(service.isOffline()).toBe(true);
  });

  it('should set isOffline false and flash justCameOnline when online event fires', () => {
    setOnline(false);
    const service = createService();
    expect(service.isOffline()).toBe(true);

    window.dispatchEvent(new Event('online'));

    expect(service.isOffline()).toBe(false);
    expect(service.justCameOnline()).toBe(true);
  });

  it('should clear justCameOnline after ONLINE_TOAST_DURATION_MS', () => {
    setOnline(false);
    const service = createService();
    window.dispatchEvent(new Event('online'));
    expect(service.justCameOnline()).toBe(true);

    vi.advanceTimersByTime(UI.ONLINE_TOAST_DURATION_MS);

    expect(service.justCameOnline()).toBe(false);
  });
});
