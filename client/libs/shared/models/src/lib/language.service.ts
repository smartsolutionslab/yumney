import { Injectable, signal } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { type LanguageCode, SUPPORTED_LANGUAGES, DEFAULT_LANGUAGE } from './language-code';

const LANGUAGE_KEY = 'yn-language';

function isSupportedLanguage(lang: string): lang is LanguageCode {
  return SUPPORTED_LANGUAGES.includes(lang as LanguageCode);
}

@Injectable({ providedIn: 'root' })
export class LanguageService {
  // Mirrors transloco's active language as a signal so components can react
  // via computed() without RxJS subscriptions. Updated in initialize() and
  // switchTo().
  readonly activeLangSignal = signal<LanguageCode>(DEFAULT_LANGUAGE);

  constructor(private transloco: TranslocoService) {}

  initialize(): void {
    const lang = this.getStoredLanguage() ?? this.detectBrowserLanguage();
    this.transloco.setActiveLang(lang);
    this.activeLangSignal.set(lang);
  }

  get activeLang(): LanguageCode {
    return this.transloco.getActiveLang() as LanguageCode;
  }

  get nextLanguage(): LanguageCode {
    const currentIndex = SUPPORTED_LANGUAGES.indexOf(this.activeLang);
    return SUPPORTED_LANGUAGES[(currentIndex + 1) % SUPPORTED_LANGUAGES.length];
  }

  switchTo(lang: LanguageCode): void {
    if (!isSupportedLanguage(lang)) {
      return;
    }

    this.transloco.setActiveLang(lang);
    localStorage.setItem(LANGUAGE_KEY, lang);
    this.activeLangSignal.set(lang);
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
