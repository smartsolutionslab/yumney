import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
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

  protected nextLanguageKey = computed(() => {
    const lang = this.languageService.nextLanguage;
    return `layout.header.language${lang[0].toUpperCase()}${lang.slice(1)}`;
  });

  onLogout(): void {
    this.authService.logout();
  }

  onSwitchLanguage(): void {
    this.languageService.switchTo(this.languageService.nextLanguage);
  }
}
