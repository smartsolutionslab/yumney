import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '@yumney/shared/api-client';
import { createAsyncState, VALIDATION, HttpErrorMap } from '@yumney/shared/models';
import { FormFieldComponent } from '@yumney/ui';

@Component({
  selector: 'yn-resend-verification',
  imports: [ReactiveFormsModule, TranslocoModule, RouterLink, FormFieldComponent],
  templateUrl: './resend-verification.component.html',
  styleUrl: './resend-verification.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResendVerificationComponent {
  private static readonly resendErrorMap: HttpErrorMap = {
    503: 'auth.resendVerification.errors.serviceUnavailable',
    default: 'auth.resendVerification.errors.generic',
  };

  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private asyncState = createAsyncState(inject(DestroyRef));

  isLoading = this.asyncState.isLoading;
  isSuccess = signal(false);
  serverError = this.asyncState.serverError;

  form = this.fb.nonNullable.group({
    email: [
      '',
      [Validators.required, Validators.email, Validators.maxLength(VALIDATION.EMAIL_MAX_LENGTH)],
    ],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email } = this.form.getRawValue();

    this.asyncState.execute(
      this.authApi.resendVerificationEmail({ email }),
      ResendVerificationComponent.resendErrorMap,
      () => this.isSuccess.set(true),
    );
  }

}
