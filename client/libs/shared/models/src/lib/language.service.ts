import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

const LANGUAGE_KEY = 'yn-language';
const SUPPORTED_LANGUAGES = ['en', 'de'];
const DEFAULT_LANGUAGE = 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  constructor(private transloco: TranslocoService) {}

  initialize(): void {
    const lang = this.getStoredLanguage() ?? this.detectBrowserLanguage();
    this.transloco.setActiveLang(lang);
  }

  get activeLang(): string {
    return this.transloco.getActiveLang();
  }

  switchTo(lang: string): void {
    if (!SUPPORTED_LANGUAGES.includes(lang)) {
      return;
    }

    this.transloco.setActiveLang(lang);
    localStorage.setItem(LANGUAGE_KEY, lang);
  }

  private getStoredLanguage(): string | null {
    const stored = localStorage.getItem(LANGUAGE_KEY);
    return stored && SUPPORTED_LANGUAGES.includes(stored) ? stored : null;
  }

  private detectBrowserLanguage(): string {
    const browserLang = navigator.language?.split('-')[0]?.toLowerCase();
    return SUPPORTED_LANGUAGES.includes(browserLang) ? browserLang : DEFAULT_LANGUAGE;
  }
}
