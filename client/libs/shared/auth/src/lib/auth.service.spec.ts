import { TestBed } from '@angular/core/testing';
import { OAuthService } from 'angular-oauth2-oidc';
import { Subject } from 'rxjs';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let oauthMock: {
    configure: ReturnType<typeof vi.fn>;
    setupAutomaticSilentRefresh: ReturnType<typeof vi.fn>;
    loadDiscoveryDocumentAndTryLogin: ReturnType<typeof vi.fn>;
    hasValidAccessToken: ReturnType<typeof vi.fn>;
    getAccessToken: ReturnType<typeof vi.fn>;
    getIdentityClaims: ReturnType<typeof vi.fn>;
    initCodeFlow: ReturnType<typeof vi.fn>;
    logOut: ReturnType<typeof vi.fn>;
    events: Subject<unknown>;
  };

  beforeEach(() => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          keycloakUrl: 'http://localhost:8080',
          keycloakRealm: 'yumney',
          keycloakClientId: 'yumney-web',
        }),
        { status: 200 },
      ),
    );

    oauthMock = {
      configure: vi.fn(),
      setupAutomaticSilentRefresh: vi.fn(),
      loadDiscoveryDocumentAndTryLogin: vi.fn().mockResolvedValue(true),
      hasValidAccessToken: vi.fn().mockReturnValue(false),
      getAccessToken: vi.fn().mockReturnValue('mock-token'),
      getIdentityClaims: vi.fn().mockReturnValue(null),
      initCodeFlow: vi.fn(),
      logOut: vi.fn(),
      events: new Subject(),
    };

    TestBed.configureTestingModule({
      providers: [AuthService, { provide: OAuthService, useValue: oauthMock }],
    });

    service = TestBed.inject(AuthService);
  });

  it('should create the service', () => {
    expect(service).toBeTruthy();
  });

  it('should return false for isAuthenticated when no token', async () => {
    oauthMock.hasValidAccessToken.mockReturnValue(false);

    await service.initialize();

    expect(service.isAuthenticated()).toBe(false);
  });

  it('should return true for isAuthenticated when valid token', async () => {
    oauthMock.hasValidAccessToken.mockReturnValue(true);
    oauthMock.getIdentityClaims.mockReturnValue({
      sub: '123',
      email: 'test@example.com',
      preferred_username: 'testuser',
      realm_access: { roles: ['user'] },
    });

    await service.initialize();

    expect(service.isAuthenticated()).toBe(true);
  });

  it('should call OAuthService.logOut on logout', () => {
    service.logout();

    expect(oauthMock.logOut).toHaveBeenCalled();
  });

  it('should return display name from claims', async () => {
    oauthMock.hasValidAccessToken.mockReturnValue(true);
    oauthMock.getIdentityClaims.mockReturnValue({
      sub: '123',
      email: 'test@example.com',
      preferred_username: 'chefheiko',
      realm_access: { roles: [] },
    });

    await service.initialize();

    expect(service.displayName()).toBe('chefheiko');
  });

  it('should return first initial for userInitial', async () => {
    oauthMock.hasValidAccessToken.mockReturnValue(true);
    oauthMock.getIdentityClaims.mockReturnValue({
      sub: '123',
      email: 'test@example.com',
      preferred_username: 'chefheiko',
      realm_access: { roles: [] },
    });

    await service.initialize();

    expect(service.userInitial()).toBe('C');
  });
});
