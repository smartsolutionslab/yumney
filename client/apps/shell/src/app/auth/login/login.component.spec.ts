import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LoginComponent } from './login.component';
import { AuthService } from '@yumney/shared/auth';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  auth: {
    login: {
      title: 'Welcome back',
      subtitle: 'Sign in to your Yumney account',
      submit: 'Sign in with Keycloak',
      rememberMe: 'Stay logged in',
      noAccount: "Don't have an account?",
      registerLink: 'Create one',
      forgotPassword: 'Forgot your password?',
    },
  },
};

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceMock: {
    login: ReturnType<typeof vi.fn>;
    forgotPassword: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    authServiceMock = { login: vi.fn(), forgotPassword: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        LoginComponent,
        setupTranslocoTesting(en),
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
    component.rememberMe.set(true);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    button.click();

    expect(authServiceMock.login).toHaveBeenCalledWith(true);
  });

  it('should contain a link to the register page', () => {
    const link = fixture.nativeElement.querySelector('a[href="/auth/register"]');
    expect(link).toBeTruthy();
  });

  it('should render the subtitle', () => {
    const subtitle = fixture.nativeElement.querySelector('.subtitle');
    expect(subtitle.textContent).toContain('Sign in to your Yumney account');
  });

  it('should have rememberMe unchecked by default', () => {
    expect(component.rememberMe()).toBe(false);
    const checkbox = fixture.nativeElement.querySelector('input[type="checkbox"]');
    expect(checkbox.checked).toBe(false);
  });

  it('should render the remember me label', () => {
    const label = fixture.nativeElement.querySelector('.remember-me');
    expect(label.textContent).toContain('Stay logged in');
  });

  it('should render forgot password link', () => {
    const link = fixture.nativeElement.querySelector('.forgot-password a');
    expect(link).toBeTruthy();
    expect(link.textContent).toContain('Forgot your password?');
  });

  it('should toggle rememberMe when checkbox is clicked', () => {
    expect(component.rememberMe()).toBe(false);

    const checkbox = fixture.nativeElement.querySelector('input[type="checkbox"]');
    checkbox.click();
    fixture.detectChanges();

    expect(component.rememberMe()).toBe(true);
  });

  it('should have accessible keyboard navigation on forgot password link', () => {
    const link = fixture.nativeElement.querySelector('.forgot-password a');
    expect(link.getAttribute('tabindex')).toBe('0');
  });

  it('should call authService.forgotPassword on forgot password click', () => {
    const link = fixture.nativeElement.querySelector('.forgot-password a');
    link.click();

    expect(authServiceMock.forgotPassword).toHaveBeenCalled();
  });
});
