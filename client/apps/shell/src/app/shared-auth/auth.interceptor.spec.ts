import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { OAuthService } from 'angular-oauth2-oidc';
import { authInterceptor } from '@yumney/shared/auth';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let oauthMock: {
    hasValidAccessToken: ReturnType<typeof vi.fn>;
    getAccessToken: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    oauthMock = {
      hasValidAccessToken: vi.fn().mockReturnValue(false),
      getAccessToken: vi.fn().mockReturnValue('test-token'),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: OAuthService, useValue: oauthMock },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should add Bearer token to /api/ requests when authenticated', () => {
    oauthMock.hasValidAccessToken.mockReturnValue(true);

    http.get('/api/v1/recipes').subscribe();

    const req = httpTesting.expectOne('/api/v1/recipes');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush([]);
  });

  it('should not add token to /api/ requests when not authenticated', () => {
    oauthMock.hasValidAccessToken.mockReturnValue(false);

    http.get('/api/v1/recipes').subscribe();

    const req = httpTesting.expectOne('/api/v1/recipes');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('should not add token to non-api requests', () => {
    oauthMock.hasValidAccessToken.mockReturnValue(true);

    http.get('/assets/i18n/en.json').subscribe();

    const req = httpTesting.expectOne('/assets/i18n/en.json');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
