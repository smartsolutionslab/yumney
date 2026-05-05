import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

interface MediaQueryStub {
  matches: boolean;
  listeners: Array<(event: MediaQueryListEvent) => void>;
  addEventListener: (type: string, listener: (event: MediaQueryListEvent) => void) => void;
  removeEventListener: (type: string, listener: (event: MediaQueryListEvent) => void) => void;
  dispatch: (matches: boolean) => void;
}

function installMatchMedia(initialDark: boolean): MediaQueryStub {
  const stub: MediaQueryStub = {
    matches: initialDark,
    listeners: [],
    addEventListener: (_, listener) => stub.listeners.push(listener),
    removeEventListener: (_, listener) => {
      stub.listeners = stub.listeners.filter((each) => each !== listener);
    },
    dispatch: (matches: boolean) => {
      stub.matches = matches;
      const event = { matches } as MediaQueryListEvent;
      stub.listeners.forEach((listener) => listener(event));
    },
  };

  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      ...stub,
      media: query,
    })),
  });

  return stub;
}

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    TestBed.resetTestingModule();
  });

  it('should default to system on first run when nothing is stored', () => {
    installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('system');
  });

  it('should restore an explicit light preference from localStorage', () => {
    localStorage.setItem('yn-theme', 'light');
    installMatchMedia(true); // OS dark, but explicit choice wins
    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('light');
    expect(service.theme()).toBe('light');
  });

  it('should restore an explicit dark preference from localStorage', () => {
    localStorage.setItem('yn-theme', 'dark');
    installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('dark');
    expect(service.theme()).toBe('dark');
  });

  it('should resolve system preference to dark when OS is dark', () => {
    localStorage.setItem('yn-theme', 'system');
    installMatchMedia(true);
    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('system');
    expect(service.theme()).toBe('dark');
  });

  it('should resolve system preference to light when OS is light', () => {
    localStorage.setItem('yn-theme', 'system');
    installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('system');
    expect(service.theme()).toBe('light');
  });

  it('should follow OS toggles mid-session when in system mode', () => {
    localStorage.setItem('yn-theme', 'system');
    const media = installMatchMedia(false);
    const service = TestBed.inject(ThemeService);
    service.initialize();

    expect(service.theme()).toBe('light');

    media.dispatch(true);
    expect(service.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');

    media.dispatch(false);
    expect(service.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('should ignore OS toggles when an explicit preference is set', () => {
    localStorage.setItem('yn-theme', 'light');
    const media = installMatchMedia(false);
    const service = TestBed.inject(ThemeService);
    service.initialize();

    media.dispatch(true);

    expect(service.theme()).toBe('light');
  });

  it('should apply the resolved theme to the document on initialize', () => {
    localStorage.setItem('yn-theme', 'dark');
    installMatchMedia(false);
    const service = TestBed.inject(ThemeService);
    service.initialize();

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('should toggle between light and dark', () => {
    installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    service.setTheme('light');
    service.toggle();
    expect(service.theme()).toBe('dark');
    expect(service.preference()).toBe('dark');

    service.toggle();
    expect(service.theme()).toBe('light');
    expect(service.preference()).toBe('light');
  });

  it('should persist the chosen preference to localStorage', () => {
    installMatchMedia(false);
    const service = TestBed.inject(ThemeService);

    service.setTheme('system');

    expect(localStorage.getItem('yn-theme')).toBe('system');
  });
});
