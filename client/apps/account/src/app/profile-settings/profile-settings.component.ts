import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import {
  UserProfileApiService,
  type UserProfile,
  type UpdateProfileRequest,
} from '@yumney/shared/api-client';

@Component({
  selector: 'yn-profile-settings',
  standalone: true,
  imports: [FormsModule, TranslocoModule],
  templateUrl: './profile-settings.component.html',
  styleUrl: './profile-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileSettingsComponent {
  private api = inject(UserProfileApiService);
  private destroyRef = inject(DestroyRef);

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
          setTimeout(() => this.saved.set(false), 3000);
        },
        error: () => {
          this.error.set('Failed to save profile');
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
        error: () => {
          this.error.set('Failed to load profile');
          this.loading.set(false);
        },
      });
  }
}
