import { Injectable, inject, computed } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';

const ROUTE_HINTS: Record<string, string> = {
  '/dashboard': 'commandBar.hints.dashboard',
  '/recipes': 'commandBar.hints.recipes',
  '/shopping': 'commandBar.hints.shopping',
  '/meal-planner': 'commandBar.hints.mealPlanner',
  '/account': 'commandBar.hints.account',
};

const DEFAULT_HINT = 'commandBar.hints.default';

@Injectable({ providedIn: 'root' })
export class ChatHintService {
  private router = inject(Router);

  private currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly hintKey = computed(() => {
    const url = this.currentUrl();
    const match = Object.keys(ROUTE_HINTS).find((route) => url.startsWith(route));
    return match ? ROUTE_HINTS[match] : DEFAULT_HINT;
  });

  readonly pageContext = computed(() => {
    const url = this.currentUrl();
    if (url.startsWith('/shopping')) return 'shopping-list';
    if (url.startsWith('/meal-planner')) return 'meal-planner';
    if (url.startsWith('/recipes')) return 'recipes';
    if (url.startsWith('/dashboard')) return 'dashboard';
    if (url.startsWith('/account')) return 'account';
    return undefined;
  });
}
