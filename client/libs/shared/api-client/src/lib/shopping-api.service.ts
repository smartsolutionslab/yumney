import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
  name: string;
  amount: number | null;
  unit: string | null;
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
    return this.http.post<ShoppingListDetail>('/api/v1/shopping-lists', request);
  }

  getShoppingLists(): Observable<ShoppingListSummary[]> {
    return this.http.get<ShoppingListSummary[]>('/api/v1/shopping-lists');
  }

  getShoppingListById(identifier: string): Observable<ShoppingListDetail> {
    return this.http.get<ShoppingListDetail>(`/api/v1/shopping-lists/${identifier}`);
  }
}
