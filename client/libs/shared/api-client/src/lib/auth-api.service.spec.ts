import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthApiService, RegisterRequest, ResendVerificationRequest } from './auth-api.service';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AuthApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('register', () => {
    it('should POST to /api/v1/auth/register', () => {
      const request: RegisterRequest = {
        email: 'test@example.com',
        password: 'SecurePass123!',
        displayName: 'Test User',
      };

      service.register(request).subscribe((result) => {
        expect(result).toEqual({ message: 'Registration successful' });
      });

      const req = httpTesting.expectOne('/api/v1/auth/register');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush({ message: 'Registration successful' });
    });
  });

  describe('resendVerificationEmail', () => {
    it('should POST to /api/v1/auth/resend-verification-email', () => {
      const request: ResendVerificationRequest = { email: 'test@example.com' };

      service.resendVerificationEmail(request).subscribe((result) => {
        expect(result).toEqual({ message: 'Email sent' });
      });

      const req = httpTesting.expectOne('/api/v1/auth/resend-verification-email');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush({ message: 'Email sent' });
    });
  });
});
