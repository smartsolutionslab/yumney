import { ERROR_MAPS } from './error-maps';

interface ErrorLeaf {
  default: string;
  [statusCode: number]: string;
}

function isLeaf(value: unknown): value is ErrorLeaf {
  return typeof value === 'object' && value !== null && 'default' in value && typeof (value as { default: unknown }).default === 'string';
}

function* walkLeaves(node: unknown, path: string[] = []): Generator<{ path: string[]; leaf: ErrorLeaf }> {
  if (!node || typeof node !== 'object') return;
  if (isLeaf(node)) {
    yield { path, leaf: node };
    return;
  }
  for (const [key, value] of Object.entries(node)) {
    yield* walkLeaves(value, [...path, key]);
  }
}

describe('ERROR_MAPS', () => {
  const leaves = Array.from(walkLeaves(ERROR_MAPS));

  it('finds at least one error leaf per top-level area', () => {
    const topLevels = new Set(leaves.map((entry) => entry.path[0]));
    expect(topLevels).toEqual(new Set(['dashboard', 'recipes', 'shopping', 'mealPlanner', 'account', 'auth']));
  });

  it('every leaf has a non-empty default fallback key', () => {
    for (const { path, leaf } of leaves) {
      expect(leaf.default, `${path.join('.')} missing default`).toBeTruthy();
      expect(typeof leaf.default).toBe('string');
    }
  });

  it('every status-code entry maps to a non-empty translation key', () => {
    for (const { path, leaf } of leaves) {
      for (const [key, value] of Object.entries(leaf)) {
        if (key === 'default') continue;
        expect(typeof value, `${path.join('.')}.${key}`).toBe('string');
        expect((value as string).length, `${path.join('.')}.${key} is empty`).toBeGreaterThan(0);
      }
    }
  });

  it('translation keys are dotted paths (no spaces, lowercase prefix)', () => {
    for (const { path, leaf } of leaves) {
      for (const [statusKey, translation] of Object.entries(leaf)) {
        const formatted = `${path.join('.')}.${statusKey} → ${translation}`;
        expect(translation, formatted).toMatch(/^[a-z][a-zA-Z0-9.]+$/);
      }
    }
  });
});
