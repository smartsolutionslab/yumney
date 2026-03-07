import { authStorageFactory } from '@yumney/shared/auth';

describe('authStorageFactory', () => {
  afterEach(() => {
    localStorage.removeItem('yn_remember_me');
  });

  it('should return localStorage when remember-me is true', () => {
    localStorage.setItem('yn_remember_me', 'true');

    const storage = authStorageFactory();

    expect(storage).toBe(localStorage);
  });

  it('should return sessionStorage when remember-me is not set', () => {
    const storage = authStorageFactory();

    expect(storage).toBe(sessionStorage);
  });
});
