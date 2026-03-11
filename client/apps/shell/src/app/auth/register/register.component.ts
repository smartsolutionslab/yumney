import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '@yumney/shared/api-client';
import {
  passwordsMatchValidator,
  hasControlError,
  mapHttpError,
  VALIDATION,
  HttpErrorMap,
} from '@yumney/shared/models';

@Component({
  selector: 'yn-register',
  imports: [ReactiveFormsModule, TranslocoModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private static readonly registerErrorMap: HttpErrorMap = {
    409: 'auth.register.errors.emailAlreadyExists',
    422: 'auth.register.errors.validationFailed',
    default: 'auth.register.errors.generic',
  };

  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(false);
  isSuccess = signal(false);
  serverError = signal<string | null>(null);

  form = this.fb.nonNullable.group(
    {
      email: [
        '',
        [Validators.required, Validators.email, Validators.maxLength(VALIDATION.EMAIL_MAX_LENGTH)],
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(VALIDATION.PASSWORD_MIN_LENGTH),
          Validators.pattern(/[A-Z]/),
          Validators.pattern(/[a-z]/),
          Validators.pattern(/[0-9]/),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
      displayName: [
        '',
        [Validators.required, Validators.maxLength(VALIDATION.DISPLAY_NAME_MAX_LENGTH)],
      ],
    },
    { validators: [passwordsMatchValidator] },
  );

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.serverError.set(null);

    const { email, password, displayName } = this.form.getRawValue();

    this.authApi
      .register({ email, password, displayName })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.isSuccess.set(true);
          this.form.reset();
        },
        error: (err: HttpErrorResponse) => {
          this.isLoading.set(false);
          this.serverError.set(mapHttpError(err, RegisterComponent.registerErrorMap));
        },
      });
  }

  hasError(field: string, error: string): boolean {
    return hasControlError(this.form, field, error);
  }
}
