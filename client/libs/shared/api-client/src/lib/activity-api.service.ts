import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type { ActivityTypeKey, RecipeActivityStats, UserActivityItem } from './user-activity';

@Injectable({ providedIn: 'root' })
export class ActivityApiService {
  private http = inject(HttpClient);

  getActivity(options: { type?: ActivityTypeKey; limit?: number } = {}): Observable<UserActivityItem[]> {
    let params = new HttpParams();
    if (options.limit != null) params = params.set('limit', options.limit.toString());
    if (options.type != null) params = params.set('type', options.type);
    return this.http.get<UserActivityItem[]>(API_ENDPOINTS.users.activity, { params });
  }

  getRecipeStats(recipeIdentifier: string): Observable<RecipeActivityStats> {
    return this.http.get<RecipeActivityStats>(
      API_ENDPOINTS.users.activityRecipeStats(recipeIdentifier),
    );
  }
}
