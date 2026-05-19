import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject, signal, untracked, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { ROUTES } from '@yumney/shared/models';
import { LucideAngularModule } from 'lucide-angular';
import { QuickActionsComponent, RecentActivityComponent, ShareToastComponent, SuggestionCardComponent } from '@yumney/ui';
import { ImportPanelComponent } from '../integrations/recipes/import-panel/import-panel.component';
import { DashboardSuggestionsService } from './dashboard-suggestions.service';

@Component({
  selector: 'yn-dashboard',
  imports: [
    TranslocoModule,
    LucideAngularModule,
    RouterLink,
    ImportPanelComponent,
    QuickActionsComponent,
    SuggestionCardComponent,
    RecentActivityComponent,
    ShareToastComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DashboardSuggestionsService],
})
export class DashboardComponent implements OnInit {
  protected readonly ROUTES = ROUTES;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private suggestionsState = inject(DashboardSuggestionsService);

  protected readonly importPanel = viewChild<ImportPanelComponent>(ImportPanelComponent);

  readonly serverError = signal<string | null>(null);
  readonly shareToast = signal<string | null>(null);
  readonly sharedUrl = signal<string | null>(null);
  private readonly initialDataIsEmpty = signal(false);
  private readonly shouldExpandImport = signal(false);

  // Re-exposed so the template (and existing tests) keep their flat access.
  quickActions = this.suggestionsState.quickActions;
  suggestions = this.suggestionsState.suggestions;
  recentActivity = this.suggestionsState.recentActivity;
  suggestionsLoading = this.suggestionsState.loading;

  private readonly queryParams = toSignal(this.route.queryParams, {
    initialValue: {} as Record<string, string>,
  });

  constructor() {
    effect(() => {
      const params = this.queryParams();
      const urlParam = params['url'] as string | undefined;
      const textParam = params['text'] as string | undefined;
      const sharedText = urlParam || textParam;

      if (!sharedText) return;

      const url = urlParam || this.extractUrl(textParam);

      if (url) {
        this.shareToast.set(url);
        this.sharedUrl.set(url);
      } else if (textParam) {
        this.serverError.set('dashboard.share.noUrlFound');
        this.shouldExpandImport.set(true);
      }
    });

    effect(() => {
      const panel = this.importPanel();
      if (!panel) return;
      if (this.shouldExpandImport() || this.initialDataIsEmpty()) {
        untracked(() => panel.expand());
      }
    });
  }

  ngOnInit(): void {
    this.suggestionsState
      .load()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ initialDataIsEmpty }) => {
        this.initialDataIsEmpty.set(initialDataIsEmpty);
      });
  }

  dismissShareToast(): void {
    this.shareToast.set(null);
  }

  onQuickAction(actionKey: string): void {
    if (actionKey === 'cook_now') {
      void this.router.navigate([ROUTES.recipes.list]);
      return;
    }
    if (actionKey === 'meal_prep') {
      const ids = (this.suggestions()?.suggestions ?? []).map((item) => item.recipeIdentifier);
      const queryParams: Record<string, string> = { multiSelect: 'true' };
      if (ids.length > 0) queryParams['preselect'] = ids.join(',');
      void this.router.navigate([ROUTES.recipes.list], { queryParams });
      return;
    }
    this.shouldExpandImport.set(true);
  }

  onRecipeSaved(payload: { identifier: string; title: string }): void {
    void this.router.navigate([ROUTES.recipes.detail(payload.identifier)]);
  }

  private extractUrl(text: string | undefined): string | null {
    if (!text) return null;
    const [url] = text.match(/https?:\/\/\S+/i) ?? [];
    return url ?? null;
  }
}
