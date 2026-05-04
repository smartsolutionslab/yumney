import {
  Component,
  ChangeDetectionStrategy,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoModule } from '@jsverse/transloco';
import { createAsyncState, ERROR_MAPS, UI } from '@yumney/shared/models';
import { AsyncStateComponent } from '@yumney/ui';
import { UserProfileApiService, type UserProfile, type UpdateProfileRequest } from '../api';

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
  private loadState = createAsyncState(this.destroyRef);
  private saveState = createAsyncState(this.destroyRef);

  protected profile = signal<UserProfile | null>(null);
  protected loading = this.loadState.isLoading;
  protected saving = this.saveState.isLoading;
  protected error = computed(() => this.loadState.serverError() ?? this.saveState.serverError());
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
    this.saved.set(false);

    const request: UpdateProfileRequest = {
      defaultServings: this.defaultServings(),
      dietaryType: this.dietaryType() || null,
      restrictions: this.restrictions(),
      minVeggieMeals: this.minVeggieMeals(),
      maxRedMeatMeals: this.maxRedMeatMeals(),
      cookingEffort: this.cookingEffort() || null,
    };

    this.saveState.execute(this.api.updateProfile(request), ERROR_MAPS.account.save, (updated) => {
      this.profile.set(updated);
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), UI.SAVED_INDICATOR_MS);
    });
  }

  protected onToggleRestriction(restriction: string): void {
    const current = this.restrictions();
    if (current.includes(restriction)) {
      this.restrictions.set(current.filter((entry) => entry !== restriction));
    } else {
      this.restrictions.set([...current, restriction]);
    }
  }

  protected onRetry(): void {
    this.loadProfile();
  }

  private loadProfile(): void {
    this.loadState.execute(this.api.getProfile(), ERROR_MAPS.account.load, (profile) => {
      this.profile.set(profile);
      this.defaultServings.set(profile.defaultServings);
      this.dietaryType.set(profile.dietaryProfile.dietaryType ?? '');
      this.restrictions.set([...profile.dietaryProfile.restrictions]);
      this.minVeggieMeals.set(profile.dietaryProfile.minVeggieMeals);
      this.maxRedMeatMeals.set(profile.dietaryProfile.maxRedMeatMeals);
      this.cookingEffort.set(profile.dietaryProfile.cookingEffort ?? '');
    });
  }
}
