import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from '@yumney/shared/api-common';
import type { UserProfile, UpdateProfileRequest } from './user-profile';

@Injectable({ providedIn: 'root' })
export class UserProfileApiService {
  private http = inject(HttpClient);

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(API_ENDPOINTS.users.profile);
  }

  updateProfile(request: UpdateProfileRequest): Observable<UserProfile> {
    return this.http.put<UserProfile>(API_ENDPOINTS.users.profile, request);
  }

  // Permanently erases the current user's account (GDPR Art. 17, US-101).
  // Returns 204 on success or 503 if Keycloak deletion fails after local data
  // is already erased — in that case the user is already logged-out for all
  // practical purposes; their PII is gone but support may need to clean up
  // the dangling Keycloak account.
  deleteAccount(): Observable<void> {
    return this.http.delete<void>(API_ENDPOINTS.users.me);
  }
}
