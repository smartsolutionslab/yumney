import type { CreateShoppingListItem } from './create-shopping-list-item';

export interface CreateShoppingListRequest {
  title: string;
  items: CreateShoppingListItem[];
  recipeIdentifier?: string;
}
