import type { PaginationParams } from '@yumney/shared/models';

export type RecipeDifficulty = 'easy' | 'medium' | 'hard';

export interface GetRecipesParams extends PaginationParams {
  sortBy?: 'Name' | 'Date';
  search?: string;
  tags?: string[];
  difficulty?: RecipeDifficulty;
  maxPrepTime?: number;
  maxCookTime?: number;
}
