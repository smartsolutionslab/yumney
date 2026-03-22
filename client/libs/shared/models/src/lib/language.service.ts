import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

export type LanguageCode = 'en' | 'de';

const LANGUAGE_KEY = 'yn-language';
const SUPPORTED_LANGUAGES: readonly LanguageCode[] = ['en', 'de'] as const;
const DEFAULT_LANGUAGE: LanguageCode = 'en';

function isSupportedLanguage(lang: string): lang is LanguageCode {
  return SUPPORTED_LANGUAGES.includes(lang as LanguageCode);
}

@Injectable({ providedIn: 'root' })
export class LanguageService {
  constructor(private transloco: TranslocoService) {}

  initialize(): void {
    const lang = this.getStoredLanguage() ?? this.detectBrowserLanguage();
    this.transloco.setActiveLang(lang);
  }

  get activeLang(): LanguageCode {
    return this.transloco.getActiveLang() as LanguageCode;
  }

  switchTo(lang: LanguageCode): void {
    if (!isSupportedLanguage(lang)) {
      return;
    }

    this.transloco.setActiveLang(lang);
    localStorage.setItem(LANGUAGE_KEY, lang);
  }

  private getStoredLanguage(): LanguageCode | null {
    const stored = localStorage.getItem(LANGUAGE_KEY);
    return stored && isSupportedLanguage(stored) ? stored : null;
  }

  private detectBrowserLanguage(): LanguageCode {
    const browserLang = navigator.language?.split('-')[0]?.toLowerCase() ?? '';
    return isSupportedLanguage(browserLang) ? browserLang : DEFAULT_LANGUAGE;
  }
}
