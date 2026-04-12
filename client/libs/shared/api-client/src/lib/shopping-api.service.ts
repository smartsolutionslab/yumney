import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type { CreateShoppingListRequest } from './create-shopping-list-request';
import type { ShoppingListDetail } from './shopping-list-detail';
import type { ShoppingListSummary } from './shopping-list-summary';
import type {
  MergedShoppingList,
  AddItemRequest,
  AddedItem,
  RemoveItemRequest,
} from './merged-shopping-list';

@Injectable({ providedIn: 'root' })
export class ShoppingApiService {
  private http = inject(HttpClient);

  createShoppingList(request: CreateShoppingListRequest): Observable<ShoppingListDetail> {
    return this.http.post<ShoppingListDetail>(API_ENDPOINTS.shoppingLists.base, request);
  }

  getShoppingLists(): Observable<ShoppingListSummary[]> {
    return this.http.get<ShoppingListSummary[]>(API_ENDPOINTS.shoppingLists.base);
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

  getMergedList(): Observable<MergedShoppingList> {
    return this.http.get<MergedShoppingList>(API_ENDPOINTS.shoppingLists.merged);
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
}
