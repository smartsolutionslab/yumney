import { TestBed } from '@angular/core/testing';
import { OAuthService } from 'angular-oauth2-oidc';
import { Subject } from 'rxjs';
import { AuthService } from '@yumney/shared/auth';

describe('AuthService', () => {
  let service: AuthService;
  let oauthMock: {
    configure: ReturnType<typeof vi.fn>;
    setupAutomaticSilentRefresh: ReturnType<typeof vi.fn>;
    loadDiscoveryDocument: ReturnType<typeof vi.fn>;
    tryLogin: ReturnType<typeof vi.fn>;
    tokenEndpoint: string | null;
    hasValidAccessToken: ReturnType<typeof vi.fn>;
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
      loadDiscoveryDocument: vi.fn().mockResolvedValue(true),
      tryLogin: vi.fn().mockResolvedValue(true),
      tokenEndpoint: null,
      hasValidAccessToken: vi.fn().mockReturnValue(false),
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

  describe('initialize', () => {
    it('should set isAuthenticated to true when token is valid', async () => {
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

    it('should set isAuthenticated to false when no valid token', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(false);

      await service.initialize();

      expect(service.isAuthenticated()).toBe(false);
    });

    it('should extract user claims into currentUser signal', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'testuser',
        realm_access: { roles: ['user', 'admin'] },
      });

      await service.initialize();

      expect(service.currentUser()).toEqual({
        sub: '123',
        email: 'test@example.com',
        preferredUsername: 'testuser',
        roles: ['user', 'admin'],
      });
    });

    it('should set isLoading to false after initialization', async () => {
      expect(service.isLoading()).toBe(true);

      await service.initialize();

      expect(service.isLoading()).toBe(false);
    });

    it('should set isLoading to false even when discovery document fails', async () => {
      oauthMock.loadDiscoveryDocument.mockRejectedValue(new Error('Network error'));

      await service.initialize();

      expect(service.isLoading()).toBe(false);
    });

    it('should not update currentUser when token is valid but claims are null', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue(null);

      await service.initialize();

      expect(service.isAuthenticated()).toBe(true);
      expect(service.currentUser()).toBeNull();
    });

    it('should default roles to empty array when realm_access is missing', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'testuser',
      });

      await service.initialize();

      expect(service.currentUser()?.roles).toEqual([]);
    });

    it('should update auth state when oauth events are emitted', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(false);

      await service.initialize();
      expect(service.isAuthenticated()).toBe(false);

      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '456',
        email: 'new@example.com',
        preferred_username: 'newuser',
        realm_access: { roles: ['user'] },
      });

      oauthMock.events.next({});

      expect(service.isAuthenticated()).toBe(true);
      expect(service.currentUser()?.preferredUsername).toBe('newuser');
    });

    it('should configure OAuthService with authConfig on initialize', async () => {
      await service.initialize();

      expect(oauthMock.configure).toHaveBeenCalledWith(
        expect.objectContaining({
          clientId: 'yumney-web',
          responseType: 'code',
          scope: 'openid profile email roles',
        }),
      );
    });

    it('should setup automatic silent refresh on initialize', async () => {
      await service.initialize();

      expect(oauthMock.setupAutomaticSilentRefresh).toHaveBeenCalled();
    });

    it('should remain unauthenticated after failed discovery', async () => {
      oauthMock.loadDiscoveryDocument.mockRejectedValue(new Error('Network error'));

      await service.initialize();

      expect(service.isAuthenticated()).toBe(false);
      expect(service.currentUser()).toBeNull();
    });

    it('should set currentUser to null when token becomes invalid via event', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'testuser',
        realm_access: { roles: ['user'] },
      });

      await service.initialize();
      expect(service.currentUser()).not.toBeNull();

      oauthMock.hasValidAccessToken.mockReturnValue(false);
      oauthMock.events.next({});

      expect(service.currentUser()).toBeNull();
    });
  });

  describe('login', () => {
    it('should call initCodeFlow on login', () => {
      service.login();

      expect(oauthMock.initCodeFlow).toHaveBeenCalled();
    });

    it('should store remember-me preference on login with rememberMe=true', () => {
      const setItemSpy = vi.spyOn(Storage.prototype, 'setItem');

      service.login(true);

      expect(setItemSpy).toHaveBeenCalledWith('yn_remember_me', 'true');
      setItemSpy.mockRestore();
    });

    it('should call initCodeFlow with no extra params by default', () => {
      service.login();

      expect(oauthMock.initCodeFlow).toHaveBeenCalledWith();
    });

    it('should remove remember-me preference on login with rememberMe=false', () => {
      localStorage.setItem('yn_remember_me', 'true');
      const removeItemSpy = vi.spyOn(Storage.prototype, 'removeItem');

      service.login(false);

      expect(removeItemSpy).toHaveBeenCalledWith('yn_remember_me');
      removeItemSpy.mockRestore();
    });
  });

  describe('displayName', () => {
    it('should return preferredUsername when available', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'testuser',
        realm_access: { roles: [] },
      });

      await service.initialize();

      expect(service.displayName()).toBe('testuser');
    });

    it('should fall back to email when preferredUsername is undefined', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: undefined,
        realm_access: { roles: [] },
      });

      await service.initialize();

      expect(service.displayName()).toBe('test@example.com');
    });

    it('should return null when no user is authenticated', () => {
      expect(service.displayName()).toBeNull();
    });
  });

  describe('shortName', () => {
    it('should extract name before @ from email', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'john.doe@example.com',
        preferred_username: undefined,
        realm_access: { roles: [] },
      });

      await service.initialize();

      expect(service.shortName()).toBe('john.doe');
    });

    it('should return first word of username when no @', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'John Doe',
        realm_access: { roles: [] },
      });

      await service.initialize();

      expect(service.shortName()).toBe('John');
    });

    it('should return null when no user is authenticated', () => {
      expect(service.shortName()).toBeNull();
    });
  });

  describe('userInitial', () => {
    it('should return uppercase first letter of shortName', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'testuser',
        realm_access: { roles: [] },
      });

      await service.initialize();

      expect(service.userInitial()).toBe('T');
    });

    it('should return null when no user is authenticated', () => {
      expect(service.userInitial()).toBeNull();
    });
  });

  describe('forgotPassword', () => {
    it('should initiate code flow with UPDATE_PASSWORD action', () => {
      service.forgotPassword();

      expect(oauthMock.initCodeFlow).toHaveBeenCalledWith('', {
        kc_action: 'UPDATE_PASSWORD',
      });
    });
  });

  describe('logout', () => {
    it('should call logOut on logout', () => {
      service.logout();

      expect(oauthMock.logOut).toHaveBeenCalled();
    });

    it('should clear currentUser and isAuthenticated on logout', async () => {
      oauthMock.hasValidAccessToken.mockReturnValue(true);
      oauthMock.getIdentityClaims.mockReturnValue({
        sub: '123',
        email: 'test@example.com',
        preferred_username: 'testuser',
        realm_access: { roles: ['user'] },
      });

      await service.initialize();
      expect(service.isAuthenticated()).toBe(true);

      service.logout();

      expect(service.isAuthenticated()).toBe(false);
      expect(service.currentUser()).toBeNull();
    });

    it('should not alter isLoading on logout', async () => {
      await service.initialize();
      expect(service.isLoading()).toBe(false);

      service.logout();

      expect(service.isLoading()).toBe(false);
    });

    it('should remove remember-me preference on logout', () => {
      const removeItemSpy = vi.spyOn(Storage.prototype, 'removeItem');

      service.logout();

      expect(removeItemSpy).toHaveBeenCalledWith('yn_remember_me');
      removeItemSpy.mockRestore();
    });
  });
});
