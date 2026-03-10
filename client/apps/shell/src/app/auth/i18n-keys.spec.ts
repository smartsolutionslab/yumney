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

  it('should contain required auth.login keys', () => {
    const requiredKeys = [
      'auth.login.title',
      'auth.login.subtitle',
      'auth.login.submit',
      'auth.login.rememberMe',
      'auth.login.noAccount',
      'auth.login.registerLink',
      'auth.login.forgotPassword',
    ];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });

  it('should contain required layout.header keys', () => {
    const requiredKeys = ['layout.header.greeting', 'layout.header.logout', 'layout.header.login'];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });

  it('should contain required dashboard keys', () => {
    const requiredKeys = ['dashboard.title', 'dashboard.welcome'];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });

  it('should contain required dashboard.import keys', () => {
    const requiredKeys = [
      'dashboard.import.title',
      'dashboard.import.subtitle',
      'dashboard.import.placeholder',
      'dashboard.import.submit',
      'dashboard.import.submitting',
      'dashboard.import.success',
      'dashboard.import.errors.urlRequired',
      'dashboard.import.errors.urlInvalid',
      'dashboard.import.errors.urlTooLong',
      'dashboard.import.errors.unreachable',
      'dashboard.import.errors.timeout',
      'dashboard.import.errors.noRecipe',
      'dashboard.import.errors.generic',
    ];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });

  it('should contain required dashboard.save keys', () => {
    const requiredKeys = [
      'dashboard.save.success',
      'dashboard.save.saving',
      'dashboard.save.errors.duplicate',
      'dashboard.save.errors.generic',
    ];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });

  it('should contain required dashboard.preview keys', () => {
    const requiredKeys = [
      'dashboard.preview.title',
      'dashboard.preview.recipeTitle',
      'dashboard.preview.description',
      'dashboard.preview.servings',
      'dashboard.preview.prepTime',
      'dashboard.preview.cookTime',
      'dashboard.preview.difficulty',
      'dashboard.preview.ingredients',
      'dashboard.preview.ingredientName',
      'dashboard.preview.amount',
      'dashboard.preview.unit',
      'dashboard.preview.addIngredient',
      'dashboard.preview.steps',
      'dashboard.preview.stepDescription',
      'dashboard.preview.addStep',
      'dashboard.preview.save',
      'dashboard.preview.discard',
      'dashboard.preview.errors.titleRequired',
      'dashboard.preview.errors.titleMaxLength',
      'dashboard.preview.errors.ingredientNameRequired',
      'dashboard.preview.errors.stepDescriptionRequired',
    ];

    for (const key of requiredKeys) {
      expect(enKeys).toContain(key);
    }
  });
});
