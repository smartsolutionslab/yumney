import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type { CreateShoppingListRequest } from './create-shopping-list-request';
import type { ShoppingListDetail } from './shopping-list-detail';
import type { ShoppingListSummary } from './shopping-list-summary';

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
}
