import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { UserProfileApiService, type UserProfile, type UpdateProfileRequest } from '../api';
import { UI } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';

@Component({
  selector: 'yn-profile-settings',
  standalone: true,
  imports: [FormsModule, TranslocoModule, AsyncStateComponent],
  templateUrl: './profile-settings.component.html',
  styleUrl: './profile-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileSettingsComponent {
  private api = inject(UserProfileApiService);
  private destroyRef = inject(DestroyRef);
  private transloco = inject(TranslocoService);

  protected profile = signal<UserProfile | null>(null);
  protected loading = signal(false);
  protected saving = signal(false);
  protected error = signal<string | null>(null);
  protected saved = signal(false);

  protected defaultServings = signal(4);
  protected dietaryType = signal<string>('');
  protected restrictions = signal<string[]>([]);
  protected minVeggieMeals = signal<number | null>(null);
  protected maxRedMeatMeals = signal<number | null>(null);
  protected cookingEffort = signal<string>('');

  protected dietaryTypes = ['omnivore', 'vegetarian', 'vegan', 'pescatarian', 'flexitarian'];
  protected availableRestrictions = [
    'gluten-free',
    'lactose-free',
    'nut-allergy',
    'egg-free',
    'soy-free',
    'shellfish-allergy',
    'halal',
    'kosher',
  ];
  protected cookingEfforts = ['quick-weekdays', 'balanced', 'elaborate-weekends'];

  constructor() {
    this.loadProfile();
  }

  protected onSave(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.saved.set(false);
    this.error.set(null);

    const request: UpdateProfileRequest = {
      defaultServings: this.defaultServings(),
      dietaryType: this.dietaryType() || null,
      restrictions: this.restrictions(),
      minVeggieMeals: this.minVeggieMeals(),
      maxRedMeatMeals: this.maxRedMeatMeals(),
      cookingEffort: this.cookingEffort() || null,
    };

    this.api
      .updateProfile(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.profile.set(updated);
          this.saving.set(false);
          this.saved.set(true);
          setTimeout(() => this.saved.set(false), UI.SAVED_INDICATOR_MS);
        },
        error: () => {
          this.error.set(this.transloco.translate('account.settings.errors.saveFailed'));
          this.saving.set(false);
        },
      });
  }

  protected onToggleRestriction(restriction: string): void {
    const current = this.restrictions();
    if (current.includes(restriction)) {
      this.restrictions.set(current.filter((r) => r !== restriction));
    } else {
      this.restrictions.set([...current, restriction]);
    }
  }

  protected onRetry(): void {
    this.loadProfile();
  }

  private loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);

    // DEBUG PROBE for #340: fire a native fetch in parallel with the
    // HttpClient call so the rendered alert reveals which one (if any)
    // returns and what the response actually was. Will be reverted once
    // the remaining E2E cluster is unblocked.
    this.runFetchProbe();

    this.api
      .getProfile()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.defaultServings.set(profile.defaultServings);
          this.dietaryType.set(profile.dietaryProfile.dietaryType ?? '');
          this.restrictions.set([...profile.dietaryProfile.restrictions]);
          this.minVeggieMeals.set(profile.dietaryProfile.minVeggieMeals);
          this.maxRedMeatMeals.set(profile.dietaryProfile.maxRedMeatMeals);
          this.cookingEffort.set(profile.dietaryProfile.cookingEffort ?? '');
          this.loading.set(false);
        },
        error: (err) => {
          const detail =
            err && typeof err === 'object'
              ? `${err.status ?? '?'} ${err.statusText ?? ''} ${err.message ?? ''}`.trim()
              : String(err);
          this.error.set(`loadFailed [HttpClient: ${detail}]`);
          this.loading.set(false);
        },
      });
  }

  private async runFetchProbe(): Promise<void> {
    const gateway =
      (window as unknown as { __yumneyAppConfig?: { gatewayUrl?: string } }).__yumneyAppConfig
        ?.gatewayUrl ?? '';
    // Skip the probe entirely when no gateway config is present (e.g. unit
    // tests). Native fetch with a relative URL throws synchronously, which
    // surfaces as an unhandled promise rejection and can fail the test run.
    if (!gateway) return;
    const token = localStorage.getItem('access_token') ?? '<no-token>';
    const url = `${gateway}/api/v1/users/me/profile`;
    const start = Date.now();
    try {
      const res = await fetch(url, {
        method: 'GET',
        credentials: 'include',
        headers: { Authorization: `Bearer ${token}` },
      });
      const ms = Date.now() - start;
      const body = await res.text();
      console.log(`[fetch-probe] ${url} -> ${res.status} ${res.statusText} in ${ms}ms`);
      console.log(`[fetch-probe] body: ${body.slice(0, 200)}`);
      this.error.update((prev) =>
        `${prev ?? ''} | fetch: ${res.status} in ${ms}ms; body=${body.slice(0, 80)}`.trim(),
      );
    } catch (e) {
      const ms = Date.now() - start;
      const msg = e instanceof Error ? `${e.name}: ${e.message}` : String(e);
      console.error(`[fetch-probe] failed in ${ms}ms: ${msg}`);
      this.error.update((prev) => `${prev ?? ''} | fetch ERR in ${ms}ms: ${msg}`.trim());
    }
  }
}
