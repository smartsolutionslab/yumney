import { TranslocoTestingModule, TranslocoTestingOptions } from '@jsverse/transloco';

/**
 * Builds a `TranslocoTestingModule` configured for the `en` lang and the
 * given translation map. Replaces the verbose `TranslocoTestingModule.forRoot`
 * boilerplate that 24+ spec files used to repeat.
 *
 * Pass `extraLangs` to register additional languages (e.g. `de`) for tests
 * that need to verify lang-switch behaviour. Each extra lang reuses the same
 * translation map — sufficient because we're asserting the active-lang code
 * path, not the localised string content.
 *
 * @example
 * await TestBed.configureTestingModule({
 *   imports: [MyComponent, setupTranslocoTesting({ greeting: 'Hello' })],
 * }).compileComponents();
 *
 * @example
 * imports: [MyComponent, setupTranslocoTesting({ greeting: 'Hello' }, ['de'])]
 */
export function setupTranslocoTesting(translations: Record<string, unknown> = {}, extraLangs: readonly string[] = []) {
  const langs: Record<string, Record<string, unknown>> = { en: translations };
  for (const lang of extraLangs) {
    langs[lang] = translations;
  }
  const options: TranslocoTestingOptions = {
    langs,
    translocoConfig: {
      availableLangs: ['en', ...extraLangs],
      defaultLang: 'en',
    },
  };
  return TranslocoTestingModule.forRoot(options);
}
