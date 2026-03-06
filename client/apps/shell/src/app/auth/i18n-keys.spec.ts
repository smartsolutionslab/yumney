import { readFileSync } from 'fs';
import { resolve } from 'path';

function flattenKeys(obj: Record<string, unknown>, prefix = ''): string[] {
  return Object.entries(obj).flatMap(([key, value]) => {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (typeof value === 'object' && value !== null) {
      return flattenKeys(value as Record<string, unknown>, fullKey);
    }
    return [fullKey];
  });
}

function loadTranslations(lang: string): Record<string, unknown> {
  const filePath = resolve(process.cwd(), `apps/shell/public/assets/i18n/${lang}.json`);
  return JSON.parse(readFileSync(filePath, 'utf-8'));
}

describe('i18n keys', () => {
  const en = loadTranslations('en');
  const de = loadTranslations('de');
  const enKeys = flattenKeys(en).sort();
  const deKeys = flattenKeys(de).sort();

  it('should have matching keys between en.json and de.json', () => {
    expect(enKeys).toEqual(deKeys);
  });

  it('should contain required auth.register keys', () => {
    const requiredKeys = [
      'auth.register.title',
      'auth.register.subtitle',
      'auth.register.email',
      'auth.register.password',
      'auth.register.confirmPassword',
      'auth.register.displayName',
      'auth.register.submit',
      'auth.register.submitting',
      'auth.register.success.title',
      'auth.register.success.message',
      'auth.register.success.resendLink',
      'auth.register.errors.emailRequired',
      'auth.register.errors.emailInvalid',
      'auth.register.errors.emailMaxLength',
      'auth.register.errors.displayNameRequired',
      'auth.register.errors.displayNameMaxLength',
      'auth.register.errors.passwordRequired',
      'auth.register.errors.passwordMinLength',
      'auth.register.errors.passwordPattern',
      'auth.register.errors.confirmPasswordRequired',
      'auth.register.errors.passwordsMismatch',
      'auth.register.errors.emailAlreadyExists',
      'auth.register.errors.validationFailed',
      'auth.register.errors.generic',
    ];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });

  it('should contain required auth.resendVerification keys', () => {
    const requiredKeys = [
      'auth.resendVerification.title',
      'auth.resendVerification.subtitle',
      'auth.resendVerification.email',
      'auth.resendVerification.submit',
      'auth.resendVerification.submitting',
      'auth.resendVerification.backToRegister',
      'auth.resendVerification.success.title',
      'auth.resendVerification.success.message',
      'auth.resendVerification.errors.emailRequired',
      'auth.resendVerification.errors.emailInvalid',
      'auth.resendVerification.errors.emailMaxLength',
      'auth.resendVerification.errors.serviceUnavailable',
      'auth.resendVerification.errors.generic',
    ];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });
});
