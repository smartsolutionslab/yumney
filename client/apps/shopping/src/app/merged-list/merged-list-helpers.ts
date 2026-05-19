import type { AddedItem, ItemSource, MergedShoppingItem, MergedShoppingList } from '../api';

export function expandKey(item: MergedShoppingItem): string {
  return `${item.itemName.toLowerCase()}|${item.unit ?? ''}`;
}

// Backend source strings: 'manual', 'chat', or a free-text recipe label.
// Map the well-known cases to a translation key; literal recipe titles fall
// through to a null so the template renders them as-is.
export function sourceTranslationKey(source: ItemSource): string | null {
  const raw = (source.source ?? '').trim().toLowerCase();
  if (raw.length === 0 || raw === 'manual' || raw === 'chat') return 'shopping.sources.manual';
  return null;
}

export function sourceLiteral(source: ItemSource): string {
  return source.source ?? '';
}

export function formatSourceQuantity(source: ItemSource, item: MergedShoppingItem): string {
  if (item.unit) return `${source.quantity} ${item.unit}`;
  return `${source.quantity}`;
}

export function applyOptimisticAdd(list: MergedShoppingList | null, added: AddedItem): MergedShoppingList {
  const current = list ?? { items: [] };
  const matchIndex = current.items.findIndex(
    (item) => item.itemName.toLowerCase() === added.itemName.toLowerCase() && item.unit === added.unit,
  );

  if (matchIndex >= 0) {
    const matched = current.items[matchIndex];
    const merged: MergedShoppingItem = {
      ...matched,
      totalQuantity: matched.totalQuantity + added.quantity,
      displayQuantity: matched.displayQuantity + added.quantity,
    };
    return { ...current, items: current.items.map((item, index) => (index === matchIndex ? merged : item)) };
  }

  return {
    ...current,
    items: [
      ...current.items,
      {
        itemName: added.itemName,
        totalQuantity: added.quantity,
        displayQuantity: added.quantity,
        unit: added.unit,
        category: added.category,
        isBought: false,
        sources: [],
      },
    ],
  };
}

export function containsAddedItem(list: MergedShoppingList, added: AddedItem): boolean {
  return list.items.some(
    (item) =>
      item.itemName.toLowerCase() === added.itemName.toLowerCase() && item.unit === added.unit && item.totalQuantity >= added.quantity,
  );
}
