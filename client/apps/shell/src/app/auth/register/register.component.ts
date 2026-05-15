import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '@yumney/shared/api-client';
import {
  passwordsMatchValidator,
  createAsyncState,
  ensureFormValid,
  VALIDATION,
  ERROR_MAPS,
  ROUTES,
} from '@yumney/shared/models';
import {
  CardComponent,
  FormFieldComponent,
  MessageBannerComponent,
  SubmitButtonComponent,
} from '@yumney/ui';

@Component({
  selector: 'yn-register',
  imports: [
    ReactiveFormsModule,
    TranslocoModule,
    RouterLink,
    CardComponent,
    FormFieldComponent,
    MessageBannerComponent,
    SubmitButtonComponent,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  protected readonly ROUTES = ROUTES;

  private formBuilder = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private asyncState = createAsyncState();

  isLoading = this.asyncState.isLoading;
  isSuccess = signal(false);
  serverError = this.asyncState.serverError;

  form = this.formBuilder.nonNullable.group(
    {
      email: [
        '',
        [
          Validators.required,
          Validators.email,
          Validators.maxLength(VALIDATION.USERS.EMAIL.MAX_LENGTH),
        ],
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(VALIDATION.USERS.PASSWORD.MIN_LENGTH),
          Validators.pattern(new RegExp(VALIDATION.USERS.PASSWORD.UPPERCASE_PATTERN)),
          Validators.pattern(new RegExp(VALIDATION.USERS.PASSWORD.LOWERCASE_PATTERN)),
          Validators.pattern(new RegExp(VALIDATION.USERS.PASSWORD.DIGIT_PATTERN)),
        ],
      ],
      confirmPassword: ['', [Validators.required]],
      displayName: [
        '',
        [Validators.required, Validators.maxLength(VALIDATION.USERS.DISPLAY_NAME.MAX_LENGTH)],
      ],
    },
    { validators: [passwordsMatchValidator] },
  );

  onSubmit(): void {
    if (!ensureFormValid(this.form)) return;

    const { email, password, displayName } = this.form.getRawValue();

    this.asyncState.execute(
      this.authApi.register({ email, password, displayName }),
      ERROR_MAPS.auth.register,
      () => {
        this.isSuccess.set(true);
        this.form.reset();
      },
    );
  }
}
