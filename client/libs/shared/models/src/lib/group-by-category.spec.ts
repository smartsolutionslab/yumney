import { groupByCategory } from './group-by-category';

interface Item {
  name: string;
  category: string;
}

describe('groupByCategory', () => {
  it('groups items by their category key', () => {
    const items: Item[] = [
      { name: 'apple', category: 'produce' },
      { name: 'milk', category: 'dairy' },
      { name: 'banana', category: 'produce' },
    ];

    const groups = groupByCategory(items, (item) => item.category);

    expect(groups).toEqual([
      { category: 'produce', items: [items[0], items[2]] },
      { category: 'dairy', items: [items[1]] },
    ]);
  });

  it('returns groups in the supplied order, skipping empty categories', () => {
    const items: Item[] = [
      { name: 'milk', category: 'dairy' },
      { name: 'apple', category: 'produce' },
    ];

    const groups = groupByCategory(items, (item) => item.category, {
      order: ['produce', 'dairy', 'frozen'] as const,
    });

    expect(groups.map((group) => group.category)).toEqual(['produce', 'dairy']);
  });

  it('returns an empty array when there are no items', () => {
    expect(groupByCategory([], (item: Item) => item.category)).toEqual([]);
  });

  it('preserves insertion order within a bucket', () => {
    const items: Item[] = [
      { name: 'a', category: 'x' },
      { name: 'b', category: 'x' },
      { name: 'c', category: 'x' },
    ];

    const groups = groupByCategory(items, (item) => item.category);

    expect(groups[0].items.map((item) => item.name)).toEqual(['a', 'b', 'c']);
  });
});
