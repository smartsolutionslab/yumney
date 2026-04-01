import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_ENDPOINTS } from './api-endpoints';
import type { RegisterRequest } from './register-request';
import type { RegisterResponse } from './register-response';
import type { ResendVerificationRequest } from './resend-verification-request';
import type { ResendVerificationResponse } from './resend-verification-response';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private http = inject(HttpClient);

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(API_ENDPOINTS.auth.register, request);
  }

  resendVerificationEmail(
    request: ResendVerificationRequest,
  ): Observable<ResendVerificationResponse> {
    return this.http.post<ResendVerificationResponse>(
      API_ENDPOINTS.auth.resendVerificationEmail,
      request,
    );
  }
}
