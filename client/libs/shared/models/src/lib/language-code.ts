export type LanguageCode = 'en' | 'de';

export const SUPPORTED_LANGUAGES: readonly LanguageCode[] = ['en', 'de'] as const;
export const DEFAULT_LANGUAGE: LanguageCode = 'en';
