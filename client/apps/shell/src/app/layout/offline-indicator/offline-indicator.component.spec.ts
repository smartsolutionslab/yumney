import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { signal } from '@angular/core';
import { OfflineIndicatorComponent, OfflineStatusService } from './offline-indicator.component';

const en = {
  layout: {
    offlineIndicator: {
      offline: 'You are offline. Some features may be unavailable.',
      backOnline: 'You are back online.',
    },
  },
};

describe('OfflineStatusService', () => {
  let service: OfflineStatusService;
  let listeners: Record<string, EventListener>;

  beforeEach(() => {
    listeners = {};
    vi.spyOn(window, 'addEventListener').mockImplementation((event, handler) => {
      listeners[event as string] = handler as EventListener;
    });
    Object.defineProperty(navigator, 'onLine', {
      value: true,
      writable: true,
      configurable: true,
    });
    service = new OfflineStatusService();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('should initialize as online when navigator.onLine is true', () => {
    expect(service.isOffline()).toBe(false);
  });

  it('should initialize as offline when navigator.onLine is false', () => {
    Object.defineProperty(navigator, 'onLine', {
      value: false,
      configurable: true,
    });
    const offlineService = new OfflineStatusService();
    expect(offlineService.isOffline()).toBe(true);
  });

  it('should set isOffline to true on offline event', () => {
    listeners['offline'](new Event('offline'));
    expect(service.isOffline()).toBe(true);
  });

  it('should set isOffline to false on online event', () => {
    listeners['offline'](new Event('offline'));
    listeners['online'](new Event('online'));
    expect(service.isOffline()).toBe(false);
  });

  it('should set justCameOnline to true on online event', () => {
    listeners['offline'](new Event('offline'));
    listeners['online'](new Event('online'));
    expect(service.justCameOnline()).toBe(true);
  });

  it('should reset justCameOnline after timeout', () => {
    vi.useFakeTimers();
    listeners['offline'](new Event('offline'));
    listeners['online'](new Event('online'));
    expect(service.justCameOnline()).toBe(true);

    vi.advanceTimersByTime(3000);
    expect(service.justCameOnline()).toBe(false);
    vi.useRealTimers();
  });
});

describe('OfflineIndicatorComponent', () => {
  let fixture: ComponentFixture<OfflineIndicatorComponent>;
  let offlineStatusMock: {
    isOffline: ReturnType<typeof signal<boolean>>;
    justCameOnline: ReturnType<typeof signal<boolean>>;
  };

  beforeEach(async () => {
    offlineStatusMock = {
      isOffline: signal(false),
      justCameOnline: signal(false),
    };

    await TestBed.configureTestingModule({
      imports: [
        OfflineIndicatorComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [{ provide: OfflineStatusService, useValue: offlineStatusMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(OfflineIndicatorComponent);
    fixture.detectChanges();
  });

  it('should not show any banner when online', () => {
    const banners = fixture.nativeElement.querySelectorAll('.offline-banner, .online-banner');
    expect(banners.length).toBe(0);
  });

  it('should show offline banner when offline', () => {
    offlineStatusMock.isOffline.set(true);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.offline-banner');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('You are offline');
  });

  it('should have role="alert" on offline banner', () => {
    offlineStatusMock.isOffline.set(true);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.offline-banner');
    expect(banner.getAttribute('role')).toBe('alert');
  });

  it('should show back-online banner when justCameOnline', () => {
    offlineStatusMock.justCameOnline.set(true);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.online-banner');
    expect(banner).toBeTruthy();
    expect(banner.textContent).toContain('back online');
  });

  it('should have role="status" on back-online banner', () => {
    offlineStatusMock.justCameOnline.set(true);
    fixture.detectChanges();

    const banner = fixture.nativeElement.querySelector('.online-banner');
    expect(banner.getAttribute('role')).toBe('status');
  });

  it('should prioritize offline banner over back-online banner', () => {
    offlineStatusMock.isOffline.set(true);
    offlineStatusMock.justCameOnline.set(true);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.offline-banner')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.online-banner')).toBeFalsy();
  });
});
