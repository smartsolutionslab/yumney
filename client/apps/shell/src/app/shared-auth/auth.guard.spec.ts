import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService, authGuard } from '@yumney/shared/auth';

describe('authGuard', () => {
  let authServiceMock: {
    isAuthenticated: ReturnType<typeof vi.fn>;
    login: ReturnType<typeof vi.fn>;
  };

  const route = {} as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  beforeEach(() => {
    authServiceMock = {
      isAuthenticated: vi.fn(),
      login: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: authServiceMock }],
    });
  });

  it('should return true when authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(result).toBe(true);
  });

  it('should call login when not authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(authServiceMock.login).toHaveBeenCalled();
  });

  it('should not call login when user is already authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(true);

    TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('should return false when not authenticated', () => {
    authServiceMock.isAuthenticated.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(result).toBe(false);
  });
});
