import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import { withFallback } from './with-fallback';
import type { UserActivityItem } from './user-activity';
import type { SuggestionsResponse } from './suggestion';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private http = inject(HttpClient);

  getRecentActivity(limit = 5): Observable<UserActivityItem[]> {
    return this.http
      .get<UserActivityItem[]>(API_ENDPOINTS.users.activity, {
        params: { limit: limit.toString() },
      })
      .pipe(withFallback<UserActivityItem[]>([]));
  }

  getSuggestions(): Observable<SuggestionsResponse> {
    return this.http
      .get<SuggestionsResponse>(API_ENDPOINTS.users.suggestions)
      .pipe(withFallback<SuggestionsResponse>({ suggestions: [], quickActions: [] }));
  }
}
