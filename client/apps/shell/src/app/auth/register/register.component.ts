import { Component, ChangeDetectionStrategy, signal, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '@yumney/shared/api-client';

@Component({
  selector: 'yn-register',
  imports: [ReactiveFormsModule, TranslocoModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private destroyRef = inject(DestroyRef);

  isLoading = signal(false);
  isSuccess = signal(false);
  serverError = signal<string | null>(null);

  private passwordsMatchValidator = (control: AbstractControl): ValidationErrors | null => {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (password && confirmPassword && password !== confirmPassword) {
      return { passwordsMismatch: true };
    }

    return null;
  };

  form = this.fb.nonNullable.group(
    {
      email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/[A-Z]/),
          Validators.pattern(/[a-z]/),
          Validators.pattern(/[0-9]/),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
      displayName: ['', [Validators.required, Validators.maxLength(200)]],
    },
    { validators: [this.passwordsMatchValidator] },
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

          if (err.status === 409) {
            this.serverError.set('auth.register.errors.emailAlreadyExists');
          } else if (err.status === 422) {
            this.serverError.set('auth.register.errors.validationFailed');
          } else {
            this.serverError.set('auth.register.errors.generic');
          }
        },
      });
  }

  hasError(field: string, error: string): boolean {
    const control = this.form.get(field);
    return !!control?.hasError(error) && !!control?.touched;
  }
}
