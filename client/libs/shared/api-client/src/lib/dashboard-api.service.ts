import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
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
      .pipe(catchError(() => of([])));
  }

  getSuggestions(): Observable<SuggestionsResponse> {
    return this.http
      .get<SuggestionsResponse>(API_ENDPOINTS.users.suggestions)
      .pipe(
        catchError(() => of({ suggestions: [], quickActions: [] } satisfies SuggestionsResponse)),
      );
  }
}
