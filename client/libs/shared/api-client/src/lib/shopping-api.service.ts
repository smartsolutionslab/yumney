import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';

export interface CreateShoppingListItem {
  name: string;
  amount: number | null;
  unit: string | null;
}

export interface CreateShoppingListRequest {
  title: string;
  items: CreateShoppingListItem[];
  recipeIdentifier?: string;
}

export interface ShoppingListItemResponse {
  identifier: string;
  name: string;
  amount: number | null;
  unit: string | null;
  isChecked: boolean;
}

export interface ShoppingListDetail {
  identifier: string;
  title: string;
  recipeIdentifier: string | null;
  createdAt: string;
  items: ShoppingListItemResponse[];
}

export interface ShoppingListSummary {
  identifier: string;
  title: string;
  itemCount: number;
  createdAt: string;
}

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
