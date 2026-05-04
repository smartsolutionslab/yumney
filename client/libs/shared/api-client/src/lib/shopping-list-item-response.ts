export interface ShoppingListItemResponse {
  identifier: string;
  name: string;
  amount: number | null;
  unit: string | null;
  category: string;
  isChecked: boolean;
}
