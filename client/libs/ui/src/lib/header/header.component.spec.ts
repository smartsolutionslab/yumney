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
      recipes: 'My Recipes',
      cookable: 'What can I cook?',
      shoppingLists: 'Shopping Lists',
      pantry: 'Pantry',
      mealPlanner: 'Meal Planner',
      logout: 'Logout',
      login: 'Login',
      openChat: 'Open chat',
      settings: 'Settings',
      switchLanguage: 'Switch language',
      switchToDark: 'Switch to dark mode',
      switchToLight: 'Switch to light mode',
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
      activeLang: 'en',
      activeLangSignal: signal('en'),
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

  function openDropdown(): void {
    isAuthenticated.set(true);
    fixture.detectChanges();
    const avatarBtn = fixture.nativeElement.querySelector('.avatar-button');
    avatarBtn.click();
    fixture.detectChanges();
  }

  // ── Anonymous user ─────────────────────────────────────────────

  it('should render the login link when user is not authenticated', () => {
    fixture.detectChanges();

    const loginLink = fixture.nativeElement.querySelector('.login-link');
    expect(loginLink.textContent).toContain('Login');
  });

  it('should not render the avatar when user is not authenticated', () => {
    fixture.detectChanges();

    const avatar = fixture.nativeElement.querySelector('.avatar-wrapper');
    expect(avatar).toBeFalsy();
  });

  it('should not render the chat button when user is not authenticated', () => {
    fixture.detectChanges();

    const chatBtn = fixture.nativeElement.querySelector('.icon-button');
    expect(chatBtn).toBeFalsy();
  });

  // ── Authenticated user ─────────────────────────────────────────

  it('should render the avatar when user is authenticated', () => {
    isAuthenticated.set(true);

    fixture.detectChanges();

    const avatar = fixture.nativeElement.querySelector('.avatar-wrapper');
    expect(avatar).toBeTruthy();
  });

  it('should render the user initial in the avatar', () => {
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

  it('should render the meal planner nav link when user is authenticated', () => {
    isAuthenticated.set(true);

    fixture.detectChanges();

    const navLinks = fixture.nativeElement.querySelectorAll('.nav-link');
    const mealPlannerLink = Array.from(navLinks).find((el) =>
      (el as HTMLElement).textContent?.includes('Meal Planner'),
    );
    expect(mealPlannerLink).toBeTruthy();
  });

  // ── Chat toggle ────────────────────────────────────────────────

  it('should call chatState.toggle when chat button is clicked', () => {
    isAuthenticated.set(true);
    fixture.detectChanges();

    const chatBtn = fixture.nativeElement.querySelector('.icon-button');
    chatBtn.click();

    expect(chatMock.toggle).toHaveBeenCalled();
  });

  // ── Dropdown menu ──────────────────────────────────────────────

  it('should open dropdown when avatar is clicked', () => {
    openDropdown();

    const dropdown = fixture.nativeElement.querySelector('.user-dropdown');
    expect(dropdown).toBeTruthy();
  });

  it('should call authService.logout from dropdown', () => {
    openDropdown();

    const logoutItem = fixture.nativeElement.querySelector('.dropdown-item--danger');
    logoutItem.click();

    expect(authMock.logout).toHaveBeenCalled();
  });

  it('should call themeService.toggle from dropdown', () => {
    openDropdown();

    const items = fixture.nativeElement.querySelectorAll('.dropdown-item');
    items[0].click();

    expect(themeMock.toggle).toHaveBeenCalled();
  });

  it('should call languageService.switchTo from dropdown', () => {
    openDropdown();

    const items = fixture.nativeElement.querySelectorAll('.dropdown-item');
    items[1].click();

    expect(languageMock.switchTo).toHaveBeenCalledWith('de');
  });

  // ── Theme icons ────────────────────────────────────────────────

  it('should render the moon icon in light theme dropdown', () => {
    theme.set('light');
    openDropdown();

    const items = fixture.nativeElement.querySelectorAll('.dropdown-item');
    const icon = items[0].querySelector('lucide-icon');
    expect(icon.getAttribute('name')).toBe('moon');
  });

  it('should render the sun icon in dark theme dropdown', () => {
    theme.set('dark');
    openDropdown();

    const items = fixture.nativeElement.querySelectorAll('.dropdown-item');
    const icon = items[0].querySelector('lucide-icon');
    expect(icon.getAttribute('name')).toBe('sun');
  });
});
