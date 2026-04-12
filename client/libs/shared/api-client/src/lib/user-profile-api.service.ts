import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
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
}
