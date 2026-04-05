import { TestBed } from '@angular/core/testing';
import { ThemeService, type Theme } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({});
    service = TestBed.inject(ThemeService);
  });

  it('should default to light when no preference stored and system is light', () => {
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
      })),
    });

    const freshService = new ThemeService();
    expect(freshService.theme()).toBe('light');
  });

  it('should apply theme to document on initialize', () => {
    service.initialize();
    expect(document.documentElement.getAttribute('data-theme')).toBe(service.theme());
  });

  it('should toggle between light and dark', () => {
    service.setTheme('light');
    service.toggle();
    expect(service.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');

    service.toggle();
    expect(service.theme()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it('should persist theme to localStorage', () => {
    service.setTheme('dark');
    expect(localStorage.getItem('yn-theme')).toBe('dark');
  });

  it('should restore theme from localStorage', () => {
    localStorage.setItem('yn-theme', 'dark');
    const freshService = new ThemeService();
    expect(freshService.theme()).toBe('dark');
  });
});
