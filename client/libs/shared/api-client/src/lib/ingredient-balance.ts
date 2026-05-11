export type Freshness = 'NotTracked' | 'Fresh' | 'UseSoon' | 'CheckIt';

export type IngredientBalanceSource = 'AtHome' | 'Staple';

export interface IngredientBalanceItem {
  itemName: string;
  quantity: number | null;
  unit: string | null;
  category: string;
  source: IngredientBalanceSource;
  freshness: Freshness;
  daysSinceBought: number | null;
}

export interface IngredientBalance {
  items: IngredientBalanceItem[];
}

export interface MarkAsFrozenRequest {
  name: string;
  unit?: string | null;
}
