import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LoginComponent } from './login.component';
import { AuthService } from '@yumney/shared/auth';

const en = {
  auth: {
    login: {
      title: 'Welcome back',
      subtitle: 'Sign in to your Yumney account',
      submit: 'Sign in with Keycloak',
      rememberMe: 'Stay logged in',
      noAccount: "Don't have an account?",
      registerLink: 'Create one',
    },
  },
};

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceMock: { login: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authServiceMock = { login: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
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

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render the login title', () => {
    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading.textContent).toContain('Welcome back');
  });

  it('should call authService.login on button click', () => {
    const button = fixture.nativeElement.querySelector('button');
    button.click();

    expect(authServiceMock.login).toHaveBeenCalledWith(false);
  });

  it('should pass rememberMe=true when checkbox is checked', () => {
    component.rememberMe = true;
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    button.click();

    expect(authServiceMock.login).toHaveBeenCalledWith(true);
  });

  it('should contain a link to the register page', () => {
    const link = fixture.nativeElement.querySelector('a[href="/auth/register"]');
    expect(link).toBeTruthy();
  });
});
