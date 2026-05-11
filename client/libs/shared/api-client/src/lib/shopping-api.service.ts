import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type { CreateShoppingListRequest } from './create-shopping-list-request';
import type { CreateShoppingListFromRecipesRequest } from './create-shopping-list-from-recipes-request';
import type { ShoppingListDetail } from './shopping-list-detail';
import type { ShoppingListSummary } from './shopping-list-summary';
import type { PagedResponse } from '@yumney/shared/models';
import type {
  MergedShoppingList,
  AddItemRequest,
  AddedItem,
  RemoveItemRequest,
} from './merged-shopping-list';
import type { IngredientBalance, MarkAsFrozenRequest } from './ingredient-balance';

@Injectable({ providedIn: 'root' })
export class ShoppingApiService {
  private http = inject(HttpClient);

  createShoppingList(request: CreateShoppingListRequest): Observable<ShoppingListDetail> {
    return this.http.post<ShoppingListDetail>(API_ENDPOINTS.shoppingLists.base, request);
  }

  createShoppingListFromRecipes(
    request: CreateShoppingListFromRecipesRequest,
  ): Observable<ShoppingListDetail> {
    return this.http.post<ShoppingListDetail>(API_ENDPOINTS.shoppingLists.fromRecipes, request);
  }

  getShoppingLists(): Observable<ShoppingListSummary[]> {
    return this.http
      .get<PagedResponse<ShoppingListSummary>>(API_ENDPOINTS.shoppingLists.base)
      .pipe(map((response) => response.items));
  }

  getShoppingListById(identifier: string): Observable<ShoppingListDetail> {
    return this.http.get<ShoppingListDetail>(API_ENDPOINTS.shoppingLists.byIdentifier(identifier));
  }

  checkOffItem(
    listIdentifier: string,
    itemIdentifier: string,
    isChecked: boolean,
  ): Observable<void> {
    return this.http.put<void>(
      API_ENDPOINTS.shoppingLists.checkItem(listIdentifier, itemIdentifier),
      { isChecked },
    );
  }

  checkOffAllItems(listIdentifier: string, isChecked: boolean): Observable<void> {
    return this.http.put<void>(API_ENDPOINTS.shoppingLists.checkAll(listIdentifier), {
      isChecked,
    });
  }

  changeItemCategory(
    listIdentifier: string,
    itemIdentifier: string,
    category: string,
  ): Observable<void> {
    return this.http.post<void>(
      API_ENDPOINTS.shoppingLists.itemCategory(listIdentifier, itemIdentifier),
      { category },
    );
  }

  getMergedList(includePastBought = false): Observable<MergedShoppingList> {
    const params = includePastBought ? { includePastBought: true } : undefined;
    return this.http.get<MergedShoppingList>(API_ENDPOINTS.shoppingLists.merged, { params });
  }

  addItem(request: AddItemRequest): Observable<AddedItem> {
    return this.http.post<AddedItem>(API_ENDPOINTS.shoppingLists.items, request);
  }

  removeItem(request: RemoveItemRequest): Observable<void> {
    return this.http.delete<void>(API_ENDPOINTS.shoppingLists.items, { body: request });
  }

  exportList(): Observable<string> {
    return this.http.get(API_ENDPOINTS.shoppingLists.export, { responseType: 'text' });
  }

  startShoppingMode(): Observable<void> {
    return this.http.post<void>(API_ENDPOINTS.shoppingLists.shoppingModeStart, {});
  }

  endShoppingMode(acceptPendingChanges: boolean): Observable<void> {
    return this.http.post<void>(API_ENDPOINTS.shoppingLists.shoppingModeEnd, {
      acceptPendingChanges,
    });
  }

  getIngredientBalance(): Observable<IngredientBalance> {
    return this.http.get<IngredientBalance>(API_ENDPOINTS.shoppingLists.balance);
  }

  markAsFrozen(request: MarkAsFrozenRequest): Observable<void> {
    return this.http.post<void>(API_ENDPOINTS.shoppingLists.itemsFreeze, request);
  }
}
