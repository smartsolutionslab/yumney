import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '@yumney/shared/auth';
import { LanguageService } from '@yumney/shared/models';

@Component({
  selector: 'yn-header',
  imports: [TranslocoModule, RouterLink],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  protected authService = inject(AuthService);
  protected languageService = inject(LanguageService);

  onLogout(): void {
    this.authService.logout();
  }

  onSwitchLanguage(): void {
    const next = this.languageService.activeLang === 'en' ? 'de' : 'en';
    this.languageService.switchTo(next);
  }
}
