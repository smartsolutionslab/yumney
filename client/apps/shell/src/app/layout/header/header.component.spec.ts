import { provideYumneyIcons } from '@yumney/ui';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { HeaderComponent } from '@yumney/ui';
import { AuthService } from '@yumney/shared/auth';
import {
  LanguageService,
  ThemeService,
  ChatStateService,
  setupTranslocoTesting,
} from '@yumney/shared/models';

const en = {
  layout: {
    header: {
      greeting: 'Hi, {{name}}',
      logout: 'Sign out',
      login: 'Sign in',
      navigation: 'Main navigation',
      recipes: 'My Recipes',
      shoppingLists: 'Shopping Lists',
      openChat: 'Open chat',
      settings: 'Settings',
      switchLanguage: 'Switch language',
      switchToDark: 'Switch to dark mode',
      switchToLight: 'Switch to light mode',
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

  beforeEach(async () => {
    authServiceMock = {
      isAuthenticated: signal(false),
      displayName: signal<string | null>(null),
      shortName: signal<string | null>(null),
      userInitial: signal<string | null>(null),
      logout: vi.fn(),
    };
    const languageServiceMock = { activeLang: 'en', nextLanguage: 'de', switchTo: vi.fn() };
    const themeServiceMock = { theme: signal('light'), toggle: vi.fn(), initialize: vi.fn() };
    const chatStateMock = { toggle: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [HeaderComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
        { provide: LanguageService, useValue: languageServiceMock },
        { provide: ThemeService, useValue: themeServiceMock },
        { provide: ChatStateService, useValue: chatStateMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function setAuthenticated(): void {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.shortName.set('testuser');
    authServiceMock.userInitial.set('T');
    authServiceMock.displayName.set('testuser');
    fixture.detectChanges();
  }

  function openDropdown(): void {
    setAuthenticated();
    const avatarBtn = fixture.nativeElement.querySelector('.avatar-button');
    avatarBtn.click();
    fixture.detectChanges();
  }

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should show login link when not authenticated', () => {
    const loginLink = fixture.nativeElement.querySelector('.login-link');

    expect(loginLink).toBeTruthy();
    expect(loginLink.textContent).toContain('Sign in');
  });

  it('should show user avatar with initial when authenticated', () => {
    setAuthenticated();

    const avatar = fixture.nativeElement.querySelector('.user-avatar');
    expect(avatar).toBeTruthy();
    expect(avatar.textContent.trim()).toBe('T');
  });

  it('should show display name in dropdown when authenticated', () => {
    openDropdown();

    const name = fixture.nativeElement.querySelector('.dropdown-name');
    expect(name.textContent.trim()).toBe('testuser');
  });

  it('should show logout in dropdown when authenticated', () => {
    openDropdown();

    const logoutItem = fixture.nativeElement.querySelector('.dropdown-item--danger');
    expect(logoutItem).toBeTruthy();
    expect(logoutItem.textContent).toContain('Sign out');
  });

  it('should call logout from dropdown', () => {
    openDropdown();

    const logoutItem = fixture.nativeElement.querySelector('.dropdown-item--danger');
    logoutItem.click();

    expect(authServiceMock.logout).toHaveBeenCalled();
  });

  it('should show brand text', () => {
    const brand = fixture.nativeElement.querySelector('.brand');

    expect(brand.textContent).toContain('Yumney');
  });

  it('should not show avatar when not authenticated', () => {
    const avatar = fixture.nativeElement.querySelector('.avatar-wrapper');

    expect(avatar).toBeNull();
  });

  it('should not show login link when authenticated', () => {
    setAuthenticated();

    const loginLink = fixture.nativeElement.querySelector('.login-link');
    expect(loginLink).toBeNull();
  });

  it('should link brand to home page', () => {
    const brand = fixture.nativeElement.querySelector('.brand');

    expect(brand.getAttribute('href')).toBe('/');
  });

  it('should update avatar reactively when userInitial changes', () => {
    setAuthenticated();
    expect(fixture.nativeElement.querySelector('.user-avatar').textContent.trim()).toBe('T');

    authServiceMock.userInitial.set('A');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.user-avatar').textContent.trim()).toBe('A');
  });
});
