import type { PagedResponse } from '@yumney/shared/models';
import type { RecipeListItem } from './recipe-list-item';

export type RecipeListResponse = PagedResponse<RecipeListItem>;
