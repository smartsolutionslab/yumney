import { Injectable, signal } from '@angular/core';

/**
 * The user's stored preference. `'system'` defers to the OS via
 * `prefers-color-scheme` and re-applies if the OS toggles mid-session.
 */
export type ThemePreference = 'light' | 'dark' | 'system';

/**
 * The resolved applied theme — what the document actually wears. Always
 * concrete; never `'system'`.
 */
export type Theme = 'light' | 'dark';

const THEME_KEY = 'yn-theme';
const THEME_ATTRIBUTE = 'data-theme';
const SYSTEM_DARK_QUERY = '(prefers-color-scheme: dark)';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  /** Stored preference (what the user picked, including `'system'`). */
  readonly preference = signal<ThemePreference>(this.resolveInitialPreference());

  /** Applied theme on the document. Driven by preference + OS state. */
  readonly theme = signal<Theme>(this.resolveAppliedTheme(this.preference()));

  private mediaQuery: MediaQueryList | null = null;
  private mediaListenerAttached = false;

  initialize(): void {
    this.applyTheme(this.theme());
    this.attachSystemListener();
  }

  /**
   * Toggle between explicit light and dark. Leaves `'system'` mode if
   * the user was on it — flips to whichever is currently *not* applied.
   */
  toggle(): void {
    const next: ThemePreference = this.theme() === 'light' ? 'dark' : 'light';
    this.setTheme(next);
  }

  setTheme(preference: ThemePreference): void {
    this.preference.set(preference);
    const applied = this.resolveAppliedTheme(preference);
    this.theme.set(applied);
    this.applyTheme(applied);
    localStorage.setItem(THEME_KEY, preference);
  }

  private resolveInitialPreference(): ThemePreference {
    const stored = localStorage.getItem(THEME_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') {
      return stored;
    }
    // First-time users get OS-following behaviour by default.
    return 'system';
  }

  private resolveAppliedTheme(preference: ThemePreference): Theme {
    if (preference === 'light' || preference === 'dark') return preference;
    return this.detectSystemTheme();
  }

  private detectSystemTheme(): Theme {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return 'light';
    }
    return window.matchMedia(SYSTEM_DARK_QUERY).matches ? 'dark' : 'light';
  }

  private attachSystemListener(): void {
    if (this.mediaListenerAttached) return;
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return;

    this.mediaQuery = window.matchMedia(SYSTEM_DARK_QUERY);
    this.mediaQuery.addEventListener('change', this.onSystemThemeChange);
    this.mediaListenerAttached = true;
  }

  private readonly onSystemThemeChange = (): void => {
    // Only react when the user has delegated to the OS — explicit choices win.
    if (this.preference() !== 'system') return;
    const applied = this.detectSystemTheme();
    this.theme.set(applied);
    this.applyTheme(applied);
  };

  private applyTheme(theme: Theme): void {
    document.documentElement.setAttribute(THEME_ATTRIBUTE, theme);
  }
}
