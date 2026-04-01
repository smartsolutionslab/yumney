import type { PaginationParams } from '@yumney/shared/models';

export interface GetRecipesParams extends PaginationParams {
  sortBy?: 'Name' | 'Date';
  search?: string;
}
