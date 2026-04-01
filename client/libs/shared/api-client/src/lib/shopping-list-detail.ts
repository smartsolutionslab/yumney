import type { ShoppingListItemResponse } from './shopping-list-item-response';

export interface ShoppingListDetail {
  identifier: string;
  title: string;
  recipeIdentifier: string | null;
  createdAt: string;
  items: ShoppingListItemResponse[];
}
