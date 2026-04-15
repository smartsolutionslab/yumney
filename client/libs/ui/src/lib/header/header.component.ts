import {
  Component,
  ChangeDetectionStrategy,
  inject,
  computed,
  signal,
  ElementRef,
  HostListener,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '@yumney/shared/auth';
import { LanguageService, ThemeService, ChatStateService, ROUTES } from '@yumney/shared/models';

@Component({
  selector: 'yn-header',
  imports: [TranslocoModule, RouterLink, LucideAngularModule],
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

  private elementRef = inject(ElementRef);

  protected menuOpen = signal(false);
  protected isDark = computed(() => this.themeService.theme() === 'dark');

  protected nextLanguageKey = computed(() => {
    const lang = this.languageService.nextLanguage;
    return `layout.header.language${lang[0].toUpperCase()}${lang.slice(1)}`;
  });

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.menuOpen()) {
      this.menuOpen.set(false);
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.menuOpen() && !this.elementRef.nativeElement.contains(event.target)) {
      this.menuOpen.set(false);
    }
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
