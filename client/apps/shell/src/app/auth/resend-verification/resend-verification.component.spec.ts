import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { ResendVerificationComponent } from './resend-verification.component';
import { AuthApiService } from '@yumney/shared/api-client';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  auth: {
    resendVerification: {
      title: 'Resend Verification Email',
      subtitle: 'Enter your email',
      email: 'Email',
      emailPlaceholder: 'you@example.com',
      submit: 'Resend',
      submitting: 'Sending...',
      backToRegister: 'Back to registration',
      success: {
        title: 'Email sent',
        message: 'Check your inbox.',
      },
      errors: {
        emailRequired: 'Email is required.',
        emailInvalid: 'Invalid email.',
        emailMaxLength: 'Email too long.',
        serviceUnavailable: 'Service unavailable.',
        generic: 'Unexpected error.',
      },
    },
  },
};

describe('ResendVerificationComponent', () => {
  let component: ResendVerificationComponent;
  let fixture: ComponentFixture<ResendVerificationComponent>;
  let authApiMock: { resendVerificationEmail: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authApiMock = { resendVerificationEmail: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ResendVerificationComponent, setupTranslocoTesting(en)],
      providers: [provideRouter([]), { provide: AuthApiService, useValue: authApiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(ResendVerificationComponent);
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
      expect(component.form.controls.email.hasError('required')).toBe(true);
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
  });

  describe('submission flow', () => {
    it('should not submit when form is invalid', () => {
      component.onSubmit();
      expect(authApiMock.resendVerificationEmail).not.toHaveBeenCalled();
    });

    it('should mark email as touched on invalid submit', () => {
      component.onSubmit();
      expect(component.form.controls.email.touched).toBe(true);
    });

    it('should set isLoading to true during submission', () => {
      component.form.controls.email.setValue('test@example.com');
      const subject = new Subject();
      authApiMock.resendVerificationEmail.mockReturnValue(subject);

      component.onSubmit();
      expect(component.isLoading()).toBe(true);

      subject.next({ message: 'ok' });
      subject.complete();
      expect(component.isLoading()).toBe(false);
    });

    it('should clear serverError on new submission', fakeAsync(() => {
      component.form.controls.email.setValue('test@example.com');
      authApiMock.resendVerificationEmail.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 500 })),
      );

      component.onSubmit();
      tick();
      expect(component.serverError()).toBe('auth.resendVerification.errors.generic');

      authApiMock.resendVerificationEmail.mockReturnValue(of({ message: 'ok' }));
      component.onSubmit();
      expect(component.serverError()).toBeNull();
    }));

    it('should set isSuccess on successful submission', fakeAsync(() => {
      component.form.controls.email.setValue('test@example.com');
      authApiMock.resendVerificationEmail.mockReturnValue(of({ message: 'ok' }));

      component.onSubmit();
      tick();

      expect(component.isSuccess()).toBe(true);
    }));

    it('should set serverError for 503 service unavailable', fakeAsync(() => {
      component.form.controls.email.setValue('test@example.com');
      authApiMock.resendVerificationEmail.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 503 })),
      );

      component.onSubmit();
      tick();

      expect(component.serverError()).toBe('auth.resendVerification.errors.serviceUnavailable');
    }));

    it('should set generic serverError for other errors', fakeAsync(() => {
      component.form.controls.email.setValue('test@example.com');
      authApiMock.resendVerificationEmail.mockReturnValue(
        throwError(() => new HttpErrorResponse({ status: 500 })),
      );

      component.onSubmit();
      tick();

      expect(component.serverError()).toBe('auth.resendVerification.errors.generic');
    }));
  });
});
