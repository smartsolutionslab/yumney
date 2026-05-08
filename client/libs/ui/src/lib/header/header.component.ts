import { Component, ChangeDetectionStrategy, inject, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '@yumney/shared/auth';
import { LanguageService, ThemeService, ChatStateService, ROUTES } from '@yumney/shared/models';
import { ClickOutsideDirective } from '../directives/click-outside.directive';

@Component({
  selector: 'yn-header',
  imports: [TranslocoModule, RouterLink, LucideAngularModule, ClickOutsideDirective],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  protected readonly ROUTES = ROUTES;

  protected authService = inject(AuthService);
  protected languageService = inject(LanguageService);
  protected themeService = inject(ThemeService);
  protected chatState = inject(ChatStateService);

  protected menuOpen = signal(false);
  protected isDark = computed(() => this.themeService.theme() === 'dark');

  protected nextLanguageKey = computed(() => {
    const lang = this.nextLanguage();
    return `layout.header.language${lang[0].toUpperCase()}${lang.slice(1)}`;
  });

  // Exposed for E2E selectors via data-current-lang / data-next-lang. Reads
  // the same signal Transloco re-renders on, so the data attributes update
  // when language changes — tests can poll without re-opening the menu.
  protected currentLanguage = computed(() => {
    // Touch the transloco signal so this re-evaluates on language change.
    void this.languageService.activeLangSignal();
    return this.languageService.activeLang;
  });
  protected nextLanguage = computed(() => {
    void this.languageService.activeLangSignal();
    return this.languageService.nextLanguage;
  });

  onDismissMenu(): void {
    if (this.menuOpen()) this.menuOpen.set(false);
  }

  onToggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  onLogout(): void {
    this.menuOpen.set(false);
    this.authService.logout();
  }

  onSwitchLanguage(): void {
    this.languageService.switchTo(this.languageService.nextLanguage);
  }

  onToggleTheme(): void {
    this.themeService.toggle();
  }

  onToggleChat(): void {
    this.chatState.toggle();
  }
}
