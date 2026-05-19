export interface ItemSource {
  quantity: number;
  source: string;
  occurredAt: string;
}

export interface MergedShoppingItem {
  itemName: string;
  totalQuantity: number;
  displayQuantity: number;
  unit: string | null;
  category: string;
  isBought: boolean;
  sources: ItemSource[];
}

export interface MergedShoppingList {
  items: MergedShoppingItem[];
}

export interface AddItemRequest {
  name: string;
  quantity?: number;
  unit?: string;
}

export interface AddedItem {
  itemName: string;
  quantity: number;
  unit: string | null;
  category: string;
  source: string;
  ledgerIdentifier: string;
}

export interface RemoveItemRequest {
  name: string;
  quantity?: number;
  unit?: string;
  reason?: string;
}
