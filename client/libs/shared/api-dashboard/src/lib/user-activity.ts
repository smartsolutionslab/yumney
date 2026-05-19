export type ActivityTypeKey =
  | 'recipe_imported'
  | 'recipe_viewed'
  | 'recipe_cooked'
  | 'recipe_edited'
  | 'recipe_deleted'
  | 'shopping_list_created';

export interface UserActivityItem {
  type: ActivityTypeKey;
  recipeIdentifier: string | null;
  recipeTitle: string | null;
  occurredAt: string;
}

export interface UserActivityPage {
  items: UserActivityItem[];
  nextCursor: string | null;
}

export interface RecipeActivityStats {
  cookCount: number;
  lastCookedAt: string | null;
  viewCount: number;
}
