import { provideYumneyIcons } from '../icons/provide-icons';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal, computed } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AuthService } from '@yumney/shared/auth';
import {
  LanguageService,
  ThemeService,
  ChatStateService,
  setupTranslocoTesting,
} from '@yumney/shared/models';
import { HeaderComponent } from './header.component';

const en = {
  layout: {
    header: {
      navigation: 'Main navigation',
      myRecipes: 'My Recipes',
      shoppingLists: 'Shopping Lists',
      logout: 'Logout',
      login: 'Login',
      openChat: 'Open chat',
      switchLanguage: 'Switch language',
      languageEn: 'EN',
      languageDe: 'DE',
    },
  },
};

describe('HeaderComponent', () => {
  let fixture: ComponentFixture<HeaderComponent>;
  let isAuthenticated: ReturnType<typeof signal<boolean>>;
  let theme: ReturnType<typeof signal<'light' | 'dark'>>;
  let authMock: Partial<AuthService>;
  let languageMock: Partial<LanguageService>;
  let themeMock: Partial<ThemeService>;
  let chatMock: { toggle: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    isAuthenticated = signal(false);
    theme = signal<'light' | 'dark'>('light');

    authMock = {
      isAuthenticated: computed(() => isAuthenticated()),
      displayName: computed(() => 'Jane Doe'),
      userInitial: computed(() => 'J'),
      shortName: computed(() => 'Jane'),
      logout: vi.fn(),
    };

    languageMock = {
      nextLanguage: 'de',
      switchTo: vi.fn(),
    };

    themeMock = {
      theme: computed(() => theme()),
      toggle: vi.fn(),
    };

    chatMock = { toggle: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [HeaderComponent, setupTranslocoTesting(en)],
      providers: [
        provideYumneyIcons(),
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
        { provide: LanguageService, useValue: languageMock },
        { provide: ThemeService, useValue: themeMock },
        { provide: ChatStateService, useValue: chatMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
  });

  // ── Anonymous user ─────────────────────────────────────────────────────────

  it('should render the login link when user is not authenticated', () => {
    fixture.detectChanges();

    const loginLink = fixture.nativeElement.querySelector('.login-link');
    expect(loginLink.textContent).toContain('Login');
  });

  it('should not render the user menu when user is not authenticated', () => {
    fixture.detectChanges();

    const userMenu = fixture.nativeElement.querySelector('.user-menu');
    expect(userMenu).toBeFalsy();
  });

  it('should not render the chat toggle when user is not authenticated', () => {
    fixture.detectChanges();

    const chatToggle = fixture.nativeElement.querySelector('.chat-toggle');
    expect(chatToggle).toBeFalsy();
  });

  // ── Authenticated user ─────────────────────────────────────────────────────

  it('should render the user menu when user is authenticated', () => {
    isAuthenticated.set(true);

    fixture.detectChanges();

    const userMenu = fixture.nativeElement.querySelector('.user-menu');
    expect(userMenu).toBeTruthy();
  });

  it('should render the user initial when user is authenticated', () => {
    isAuthenticated.set(true);

    fixture.detectChanges();

    const avatar = fixture.nativeElement.querySelector('.user-avatar');
    expect(avatar.textContent).toContain('J');
  });

  it('should render the recipes nav link when user is authenticated', () => {
    isAuthenticated.set(true);

    fixture.detectChanges();

    const navLinks = fixture.nativeElement.querySelectorAll('.nav-link');
    const recipeLink = Array.from(navLinks).find((el) =>
      (el as HTMLElement).textContent?.includes('My Recipes'),
    );
    expect(recipeLink).toBeTruthy();
  });

  it('should render the shopping nav link when user is authenticated', () => {
    isAuthenticated.set(true);

    fixture.detectChanges();

    const navLinks = fixture.nativeElement.querySelectorAll('.nav-link');
    const shoppingLink = Array.from(navLinks).find((el) =>
      (el as HTMLElement).textContent?.includes('Shopping Lists'),
    );
    expect(shoppingLink).toBeTruthy();
  });

  // ── User actions ───────────────────────────────────────────────────────────

  it('should call authService.logout when logout button is clicked', () => {
    isAuthenticated.set(true);
    fixture.detectChanges();

    const logoutBtn = fixture.nativeElement.querySelector('.logout-button');
    logoutBtn.click();

    expect(authMock.logout).toHaveBeenCalled();
  });

  it('should call languageService.switchTo when language toggle is clicked', () => {
    fixture.detectChanges();

    const langBtn = fixture.nativeElement.querySelector('.lang-toggle');
    langBtn.click();

    expect(languageMock.switchTo).toHaveBeenCalledWith('de');
  });

  it('should call themeService.toggle when theme toggle is clicked', () => {
    fixture.detectChanges();

    const themeBtn = fixture.nativeElement.querySelector('.theme-toggle');
    themeBtn.click();

    expect(themeMock.toggle).toHaveBeenCalled();
  });

  it('should call chatState.toggle when chat toggle is clicked', () => {
    isAuthenticated.set(true);
    fixture.detectChanges();

    const chatBtn = fixture.nativeElement.querySelector('.chat-toggle');
    chatBtn.click();

    expect(chatMock.toggle).toHaveBeenCalled();
  });

  // ── Theme display ─────────────────────────────────────────────────────────

  it('should render the moon icon in light theme', () => {
    theme.set('light');

    fixture.detectChanges();

    const themeBtn = fixture.nativeElement.querySelector('.theme-toggle');
    const icon = themeBtn.querySelector('lucide-icon');
    expect(icon).toBeTruthy();
    expect(icon.getAttribute('name')).toBe('moon');
  });

  it('should render the sun icon in dark theme', () => {
    theme.set('dark');

    fixture.detectChanges();

    const themeBtn = fixture.nativeElement.querySelector('.theme-toggle');
    const icon = themeBtn.querySelector('lucide-icon');
    expect(icon).toBeTruthy();
    expect(icon.getAttribute('name')).toBe('sun');
  });
});
