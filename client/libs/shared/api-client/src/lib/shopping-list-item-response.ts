export interface ShoppingListItemResponse {
  identifier: string;
  name: string;
  amount: number | null;
  unit: string | null;
  isChecked: boolean;
}
