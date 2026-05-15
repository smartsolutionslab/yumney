import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '@yumney/shared/api-client';
import { createAsyncState, ensureFormValid, VALIDATION, ERROR_MAPS, ROUTES } from '@yumney/shared/models';
import { CardComponent, FormFieldComponent, MessageBannerComponent, SubmitButtonComponent } from '@yumney/ui';

@Component({
  selector: 'yn-resend-verification',
  imports: [
    ReactiveFormsModule,
    TranslocoModule,
    RouterLink,
    CardComponent,
    FormFieldComponent,
    MessageBannerComponent,
    SubmitButtonComponent,
  ],
  templateUrl: './resend-verification.component.html',
  styleUrl: './resend-verification.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResendVerificationComponent {
  protected readonly ROUTES = ROUTES;

  private formBuilder = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private asyncState = createAsyncState();

  isLoading = this.asyncState.isLoading;
  isSuccess = signal(false);
  serverError = this.asyncState.serverError;

  form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(VALIDATION.USERS.EMAIL.MAX_LENGTH)]],
  });

  onSubmit(): void {
    if (!ensureFormValid(this.form)) return;

    const { email } = this.form.getRawValue();

    this.asyncState.execute(this.authApi.resendVerificationEmail({ email }), ERROR_MAPS.auth.resendVerification, () =>
      this.isSuccess.set(true),
    );
  }
}
