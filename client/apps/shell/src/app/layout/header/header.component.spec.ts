import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { HeaderComponent } from './header.component';
import { AuthService } from '@yumney/shared/auth';

const en = {
  layout: {
    header: {
      greeting: 'Hi, {{name}}',
      logout: 'Sign out',
      login: 'Sign in',
    },
  },
};

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let authServiceMock: {
    isAuthenticated: ReturnType<typeof signal<boolean>>;
    displayName: ReturnType<typeof signal<string | null>>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceMock = {
      isAuthenticated: signal(false),
      displayName: signal<string | null>(null),
      logout: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [
        HeaderComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceMock }],
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
    authServiceMock.displayName.set('testuser');
    fixture.detectChanges();

    const logoutButton = fixture.nativeElement.querySelector('.logout-button');
    expect(logoutButton).toBeTruthy();
    expect(logoutButton.textContent).toContain('Sign out');
  });

  it('should show greeting with display name when authenticated', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.displayName.set('testuser');
    fixture.detectChanges();

    const greeting = fixture.nativeElement.querySelector('.greeting');
    expect(greeting.textContent).toContain('Hi, testuser');
  });

  it('should call logout on logout button click', () => {
    authServiceMock.isAuthenticated.set(true);
    authServiceMock.displayName.set('testuser');
    fixture.detectChanges();

    const logoutButton = fixture.nativeElement.querySelector('.logout-button');
    logoutButton.click();

    expect(authServiceMock.logout).toHaveBeenCalled();
  });

  it('should show brand text', () => {
    const brand = fixture.nativeElement.querySelector('.brand');
    expect(brand.textContent).toContain('Yumney');
  });
});
