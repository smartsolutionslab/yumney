import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { of, Subject, throwError } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthApiService } from '@yumney/shared/api-client';

const en = {
  auth: {
    register: {
      title: 'Create Account',
      subtitle: 'Join Yumney',
      email: 'Email',
      emailPlaceholder: 'you@example.com',
      displayName: 'Display Name',
      displayNamePlaceholder: 'Your name',
      password: 'Password',
      confirmPassword: 'Confirm Password',
      submit: 'Create Account',
      submitting: 'Creating account...',
      success: {
        title: 'Check your email',
        message: 'Verification link sent.',
        resendLink: 'Resend verification email',
      },
      errors: {
        emailRequired: 'Email is required.',
        emailInvalid: 'Please enter a valid email.',
        emailMaxLength: 'Email too long.',
        displayNameRequired: 'Display name is required.',
        displayNameMaxLength: 'Display name too long.',
        passwordRequired: 'Password is required.',
        passwordMinLength: 'Password too short.',
        passwordPattern: 'Password needs pattern.',
        confirmPasswordRequired: 'Confirm password.',
        passwordsMismatch: 'Passwords do not match.',
        emailAlreadyExists: 'Email already exists.',
        validationFailed: 'Validation failed.',
        generic: 'Unexpected error.',
      },
    },
  },
};

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authApiMock: { register: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authApiMock = { register: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [
        RegisterComponent,
        TranslocoTestingModule.forRoot({
          langs: { en },
          translocoConfig: {
            availableLangs: ['en'],
            defaultLang: 'en',
          },
        }),
      ],
      providers: [provideRouter([]), { provide: AuthApiService, useValue: authApiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('form validation', () => {
    it('should create the component', () => {
      expect(component).toBeTruthy();
    });

    it('should have an invalid form when empty', () => {
      expect(component.form.valid).toBe(false);
    });

    it('should require email', () => {
      const email = component.form.controls.email;
      expect(email.hasError('required')).toBe(true);
    });

    it('should reject invalid email format', () => {
      component.form.controls.email.setValue('not-an-email');
      expect(component.form.controls.email.hasError('email')).toBe(true);
    });

    it('should accept valid email', () => {
      component.form.controls.email.setValue('test@example.com');
      expect(component.form.controls.email.valid).toBe(true);
    });

    it('should reject email exceeding max length', () => {
      const longEmail = 'a'.repeat(243) + '@example.com';
      component.form.controls.email.setValue(longEmail);
      expect(component.form.controls.email.hasError('maxlength')).toBe(true);
    });

    it('should require password', () => {
      expect(component.form.controls.password.hasError('required')).toBe(true);
    });

    it('should reject short password', () => {
      component.form.controls.password.setValue('Ab1');
      expect(component.form.controls.password.hasError('minlength')).toBe(true);
    });

    it('should reject password without pattern match', () => {
      component.form.controls.password.setValue('alllowercase1');
      expect(component.form.controls.password.hasError('pattern')).toBe(true);
    });

    it('should require display name', () => {
      expect(component.form.controls.displayName.hasError('required')).toBe(true);
    });

    it('should reject display name exceeding max length', () => {
      component.form.controls.displayName.setValue('A'.repeat(201));
      expect(component.form.controls.displayName.hasError('maxlength')).toBe(true);
    });

    it('should require confirm password', () => {
      expect(component.form.controls.confirmPassword.hasError('required')).toBe(true);
    });

    it('should detect password mismatch', () => {
      component.form.controls.password.setValue('Password1');
      component.form.controls.confirmPassword.setValue('Different1');
      expect(component.form.hasError('passwordsMismatch')).toBe(true);
    });

    it('should accept matching passwords', () => {
      component.form.controls.password.setValue('Password1');
      component.form.controls.confirmPassword.setValue('Password1');
      expect(component.form.hasError('passwordsMismatch')).toBe(false);
    });
  });

  describe('submission flow', () => {
    function fillValidForm(): void {
      component.form.controls.email.setValue('test@example.com');
      component.form.controls.password.setValue('Password1');
      component.form.controls.confirmPassword.setValue('Password1');
      component.form.controls.displayName.setValue('Test User');
    }

    it('should not submit when form is invalid', () => {
      component.onSubmit();
      expect(authApiMock.register).not.toHaveBeenCalled();
    });

    it('should mark all fields as touched on invalid submit', () => {
      component.onSubmit();
      expect(component.form.controls.email.touched).toBe(true);
      expect(component.form.controls.password.touched).toBe(true);
    });

    it('should set isLoading to true during submission', () => {
      fillValidForm();
      const subject = new Subject();
      authApiMock.register.mockReturnValue(subject);

      component.onSubmit();
      expect(component.isLoading()).toBe(true);

      subject.next({ message: 'ok' });
      subject.complete();
      expect(component.isLoading()).toBe(false);
    });

    it('should set isSuccess on successful registration', fakeAsync(() => {
      fillValidForm();
      authApiMock.register.mockReturnValue(of({ message: 'ok' }));

      component.onSubmit();
      tick();

      expect(component.isSuccess()).toBe(true);
    }));

    it('should reset form on successful registration', fakeAsync(() => {
      fillValidForm();
      authApiMock.register.mockReturnValue(of({ message: 'ok' }));

      component.onSubmit();
      tick();

      expect(component.form.controls.email.value).toBe('');
      expect(component.form.controls.password.value).toBe('');
    }));

    it('should clear serverError on new submission', fakeAsync(() => {
      fillValidForm();
      authApiMock.register.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 500 })),
      );

      component.onSubmit();
      tick();
      expect(component.serverError()).toBe('auth.register.errors.generic');

      authApiMock.register.mockReturnValue(of({ message: 'ok' }));
      component.onSubmit();
      expect(component.serverError()).toBeNull();
    }));

    it('should set serverError for 409 conflict', fakeAsync(() => {
      fillValidForm();
      authApiMock.register.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 409 })),
      );

      component.onSubmit();
      tick();

      expect(component.serverError()).toBe('auth.register.errors.emailAlreadyExists');
    }));

    it('should set serverError for 422 validation error', fakeAsync(() => {
      fillValidForm();
      authApiMock.register.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 422 })),
      );

      component.onSubmit();
      tick();

      expect(component.serverError()).toBe('auth.register.errors.validationFailed');
    }));

    it('should set generic serverError for 500', fakeAsync(() => {
      fillValidForm();
      authApiMock.register.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 500 })),
      );

      component.onSubmit();
      tick();

      expect(component.serverError()).toBe('auth.register.errors.generic');
    }));
  });
});
