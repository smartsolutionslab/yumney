import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '@yumney/shared/auth';

@Component({
  selector: 'yn-login',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private authService = inject(AuthService);

  rememberMe = signal(false);

  onLogin(): void {
    this.authService.login(this.rememberMe());
  }

  onRememberMeChange(event: Event): void {
    this.rememberMe.set((event.target as HTMLInputElement).checked);
  }

  onForgotPassword(): void {
    this.authService.forgotPassword();
  }
}
