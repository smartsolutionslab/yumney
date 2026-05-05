import { BRING_FALLBACK_URL, buildBringImportUrl } from './bring-link';

describe('buildBringImportUrl', () => {
  it('returns bring://import when no items are supplied', () => {
    expect(buildBringImportUrl([])).toBe('bring://import');
  });

  it('skips items with empty or whitespace-only names', () => {
    const url = buildBringImportUrl([
      { name: '  ' },
      { name: '' },
      { name: 'Apples' },
    ]);
    expect(url).toBe('bring://import?items=Apples');
  });

  it('formats items with amount + unit', () => {
    const url = buildBringImportUrl([{ name: 'Flour', amount: 500, unit: 'g' }]);
    expect(url).toBe(`bring://import?items=${encodeURIComponent('500 g Flour')}`);
  });

  it('formats items with amount but no unit', () => {
    const url = buildBringImportUrl([{ name: 'Eggs', amount: 6, unit: null }]);
    expect(url).toBe(`bring://import?items=${encodeURIComponent('6 Eggs')}`);
  });

  it('formats items with only a name', () => {
    const url = buildBringImportUrl([{ name: 'Salt' }]);
    expect(url).toBe('bring://import?items=Salt');
  });

  it('joins multiple items with a comma', () => {
    const url = buildBringImportUrl([
      { name: 'Flour', amount: 500, unit: 'g' },
      { name: 'Salt' },
    ]);
    expect(url).toBe(
      `bring://import?items=${encodeURIComponent('500 g Flour')},Salt`,
    );
  });

  it('URL-encodes special characters in item names', () => {
    const url = buildBringImportUrl([{ name: 'Crème fraîche', amount: 200, unit: 'g' }]);
    expect(url).toContain(encodeURIComponent('200 g Crème fraîche'));
  });

  it('exposes a stable fallback URL pointing at getbring.com', () => {
    expect(BRING_FALLBACK_URL).toBe('https://www.getbring.com/');
  });
});
