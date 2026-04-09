import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { HeaderComponent } from '@yumney/ui';
import { AuthService } from '@yumney/shared/auth';
import { LanguageService, ThemeService, setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  layout: {
    header: {
      greeting: 'Hi, {{name}}',
      logout: 'Sign out',
      login: 'Sign in',
      navigation: 'Main navigation',
      switchLanguage: 'Switch language',
      languageDe: 'DE',
      languageEn: 'EN',
    },
  },
};

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let authServiceMock: {
    isAuthenticated: ReturnType<typeof signal<boolean>>;
    displayName: ReturnType<typeof signal<string | null>>;
    shortName: ReturnType<typeof signal<string | null>>;
    userInitial: ReturnType<typeof signal<string | null>>;
    logout: ReturnType<typeof vi.fn>;
  };
  let languageServiceMock: {
    activeLang: string;
    nextLanguage: string;
    switchTo: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceMock = {
      isAuthenticated: signal(false),
      displayName: signal<string | null>(null),
      shortName: signal<string | null>(null),
      userInitial: signal<string | null>(null),
      logout: vi.fn(),
    };
    languageServiceMock = { activeLang: 'en', nextLanguage: 'de', switchTo: vi.fn() };
    const themeServiceMock = { theme: signal('light'), toggle: vi.fn(), initialize: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [HeaderComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
        { provide: LanguageService, useValue: languageServiceMock },
        { provide: ThemeService, useValue: themeServiceMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should show login link when not authenticated', () => {
    const loginLink = fixture.nativeElement.querySelector('.login-link');
    expect(loginLink).toBeTruthy();
    expect(loginLink.textContent).toContain('Sign in');
  });

  it('should show logout button when authenticated', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('testuser');
    authServiceMock.userInitial.set('T');
    fixture.detectChanges();

    const logoutButton = fixture.nativeElement.querySelector('.logout-button');
    expect(logoutButton).toBeTruthy();
    expect(logoutButton.textContent).toContain('Sign out');
  });

  it('should show user avatar with initial when authenticated', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('testuser');
    authServiceMock.userInitial.set('T');
    authServiceMock.displayName.set('testuser');
    fixture.detectChanges();

    const avatar = fixture.nativeElement.querySelector('.user-avatar');
    expect(avatar).toBeTruthy();
    expect(avatar.textContent.trim()).toBe('T');
  });

  it('should show short name when authenticated', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('testuser');
    authServiceMock.userInitial.set('T');
    fixture.detectChanges();

    const userName = fixture.nativeElement.querySelector('.user-name');
    expect(userName.textContent.trim()).toBe('testuser');
  });

  it('should call logout on logout button click', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('testuser');
    authServiceMock.userInitial.set('T');
    fixture.detectChanges();

    const logoutButton = fixture.nativeElement.querySelector('.logout-button');
    logoutButton.click();

    expect(authServiceMock.logout).toHaveBeenCalled();
  });

  it('should show brand text', () => {
    const brand = fixture.nativeElement.querySelector('.brand');
    expect(brand.textContent).toContain('Yumney');
  });

  it('should not show logout button when not authenticated', () => {
    const logoutButton = fixture.nativeElement.querySelector('.logout-button');
    expect(logoutButton).toBeNull();
  });

  it('should not show login link when authenticated', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('testuser');
    authServiceMock.userInitial.set('T');
    fixture.detectChanges();

    const loginLink = fixture.nativeElement.querySelector('.login-link');
    expect(loginLink).toBeNull();
  });

  it('should link brand to home page', () => {
    const brand = fixture.nativeElement.querySelector('.brand');
    expect(brand.getAttribute('href')).toBe('/');
  });

  it('should update user name reactively when shortName changes', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('Alice');
    authServiceMock.userInitial.set('A');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.user-name').textContent.trim()).toBe('Alice');

    authServiceMock.shortName.set('Bob');
    authServiceMock.userInitial.set('B');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.user-name').textContent.trim()).toBe('Bob');
  });

  it('should not show user menu when not authenticated', () => {
    const userMenu = fixture.nativeElement.querySelector('.user-menu');
    expect(userMenu).toBeNull();
  });
});
