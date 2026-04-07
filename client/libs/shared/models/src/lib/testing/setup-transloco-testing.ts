import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';

/**
 * Builds a `TranslocoTestingModule` configured for the `en` lang and the
 * given translation map. Replaces the verbose `TranslocoTestingModule.forRoot`
 * boilerplate that 24+ spec files used to repeat.
 *
 * @example
 * await TestBed.configureTestingModule({
 *   imports: [MyComponent, setupTranslocoTesting({ greeting: 'Hello' })],
 * }).compileComponents();
 */
export function setupTranslocoTesting(translations: Record<string, unknown> = {}) {
  const options: TranslocoTestingOptions = {
    langs: { en: translations },
    translocoConfig: {
      availableLangs: ['en'],
      defaultLang: 'en',
    },
  };
  return TranslocoTestingModule.forRoot(options);
}
