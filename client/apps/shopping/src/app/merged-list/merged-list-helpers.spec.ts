import type { AddedItem, MergedShoppingList } from '../api';
import {
  applyOptimisticAdd,
  containsAddedItem,
  expandKey,
  formatSourceQuantity,
  sourceLiteral,
  sourceTranslationKey,
} from './merged-list-helpers';

const list: MergedShoppingList = {
  items: [
    {
      itemName: 'Eggs',
      totalQuantity: 6,
      displayQuantity: 6,
      unit: null,
      category: 'dairy',
      isBought: false,
      sources: [],
    },
    {
      itemName: 'Flour',
      totalQuantity: 500,
      displayQuantity: 500,
      unit: 'g',
      category: 'pantry',
      isBought: false,
      sources: [],
    },
  ],
};

describe('sourceTranslationKey', () => {
  it('maps "manual" to the manual key', () => {
    expect(sourceTranslationKey({ source: 'manual', quantity: 1 })).toBe('shopping.sources.manual');
  });

  it('maps "chat" to the manual key', () => {
    expect(sourceTranslationKey({ source: 'chat', quantity: 1 })).toBe('shopping.sources.manual');
  });

  it('maps null/empty/whitespace source to the manual key', () => {
    expect(sourceTranslationKey({ source: null, quantity: 1 })).toBe('shopping.sources.manual');
    expect(sourceTranslationKey({ source: '   ', quantity: 1 })).toBe('shopping.sources.manual');
  });

  it('returns null for free-text recipe labels', () => {
    expect(sourceTranslationKey({ source: 'Pasta Carbonara', quantity: 1 })).toBeNull();
  });

  it('is case-insensitive for the well-known cases', () => {
    expect(sourceTranslationKey({ source: 'MANUAL', quantity: 1 })).toBe('shopping.sources.manual');
  });
});

describe('sourceLiteral', () => {
  it('returns the raw source string', () => {
    expect(sourceLiteral({ source: 'Pasta', quantity: 1 })).toBe('Pasta');
  });

  it('returns empty string when source is null', () => {
    expect(sourceLiteral({ source: null, quantity: 1 })).toBe('');
  });
});

describe('formatSourceQuantity', () => {
  it('appends the unit when the item has one', () => {
    expect(formatSourceQuantity({ source: 'manual', quantity: 200 }, list.items[1])).toBe('200 g');
  });

  it('omits the unit when the item has none', () => {
    expect(formatSourceQuantity({ source: 'manual', quantity: 6 }, list.items[0])).toBe('6');
  });
});

describe('expandKey', () => {
  it('lowercases the name and includes the unit', () => {
    expect(expandKey(list.items[1])).toBe('flour|g');
  });

  it('uses empty unit for null', () => {
    expect(expandKey(list.items[0])).toBe('eggs|');
  });
});

describe('applyOptimisticAdd', () => {
  it('merges into an existing item with the same name+unit', () => {
    const added: AddedItem = { itemName: 'Eggs', quantity: 4, unit: null, category: 'dairy' };

    const result = applyOptimisticAdd(list, added);

    expect(result.items).toHaveLength(2);
    expect(result.items[0]).toMatchObject({ itemName: 'Eggs', totalQuantity: 10, displayQuantity: 10 });
  });

  it('appends a new item when no match exists', () => {
    const added: AddedItem = { itemName: 'Salt', quantity: 1, unit: 'tsp', category: 'pantry' };

    const result = applyOptimisticAdd(list, added);

    expect(result.items).toHaveLength(3);
    expect(result.items[2]).toMatchObject({ itemName: 'Salt', totalQuantity: 1, unit: 'tsp' });
  });

  it('matches case-insensitively on name', () => {
    const added: AddedItem = { itemName: 'EGGS', quantity: 2, unit: null, category: 'dairy' };

    const result = applyOptimisticAdd(list, added);

    expect(result.items).toHaveLength(2);
    expect(result.items[0].totalQuantity).toBe(8);
  });

  it('treats different units as distinct items', () => {
    const added: AddedItem = { itemName: 'Flour', quantity: 1, unit: 'kg', category: 'pantry' };

    const result = applyOptimisticAdd(list, added);

    expect(result.items).toHaveLength(3);
  });

  it('handles a null initial list', () => {
    const added: AddedItem = { itemName: 'Eggs', quantity: 2, unit: null, category: 'dairy' };

    const result = applyOptimisticAdd(null, added);

    expect(result.items).toHaveLength(1);
    expect(result.items[0].itemName).toBe('Eggs');
  });
});

describe('containsAddedItem', () => {
  it('returns true when the list contains the added item with at least the added quantity', () => {
    expect(containsAddedItem(list, { itemName: 'Eggs', quantity: 6, unit: null, category: 'dairy' })).toBe(true);
  });

  it('returns false when totalQuantity is below the added quantity', () => {
    expect(containsAddedItem(list, { itemName: 'Eggs', quantity: 10, unit: null, category: 'dairy' })).toBe(false);
  });

  it('matches case-insensitively', () => {
    expect(containsAddedItem(list, { itemName: 'EGGS', quantity: 6, unit: null, category: 'dairy' })).toBe(true);
  });

  it('requires unit match', () => {
    expect(containsAddedItem(list, { itemName: 'Flour', quantity: 1, unit: 'kg', category: 'pantry' })).toBe(false);
  });
});
