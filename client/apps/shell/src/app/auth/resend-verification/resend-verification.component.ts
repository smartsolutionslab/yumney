import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '@yumney/shared/api-client';

@Component({
  selector: 'yn-resend-verification',
  imports: [ReactiveFormsModule, TranslocoModule, RouterLink],
  templateUrl: './resend-verification.component.html',
  styleUrl: './resend-verification.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResendVerificationComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(false);
  isSuccess = signal(false);
  serverError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.serverError.set(null);

    const { email } = this.form.getRawValue();

    this.authApi
      .resendVerificationEmail({ email })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.isSuccess.set(true);
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);

          if (err.status === 503) {
            this.serverError.set('auth.resendVerification.errors.serviceUnavailable');
          } else {
            this.serverError.set('auth.resendVerification.errors.generic');
          }
        },
      });
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!control?.hasError(error) && !!control?.touched;
  }
}
