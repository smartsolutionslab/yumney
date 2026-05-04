import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { provideYumneyIcons } from '@yumney/ui';
import { ProfileSettingsComponent } from './profile-settings.component';
import { UserProfileApiService, type UserProfile } from '../api';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  account: {
    settings: {
      title: 'Profile Settings',
      loading: 'Loading profile…',
      retry: 'Retry',
      save: 'Save',
      saving: 'Saving…',
      saved: 'Saved',
      none: 'None',

      identity: 'Identity',
      identityHint: 'Identity hint',
      displayName: 'Display name',
      email: 'Email',
      emailHint: 'Email hint',

      languageUnits: 'Language & Units',
      languageUnitsHint: '',
      preferredLanguage: 'Preferred language',
      preferredUnitSystem: 'Unit system',
      lang: { en: 'English', de: 'German' },
      unitSystem: { metric: 'Metric', imperial: 'Imperial' },

      appearance: 'Appearance',
      appearanceHint: '',
      theme: 'Theme',
      themes: { light: 'Light', dark: 'Dark', system: 'System' },

      cooking: 'Cooking',
      cookingHint: '',
      household: 'Household',
      defaultServings: 'Default servings',
      dietary: 'Dietary',
      dietaryType: 'Dietary type',
      dietaryTypes: {
        omnivore: 'Omnivore',
        vegetarian: 'Vegetarian',
        vegan: 'Vegan',
        pescatarian: 'Pescatarian',
        flexitarian: 'Flexitarian',
      },
      restrictions: 'Restrictions',
      restriction: {
        'gluten-free': 'Gluten-free',
        'lactose-free': 'Lactose-free',
        'nut-allergy': 'Nut allergy',
        'egg-free': 'Egg-free',
        'soy-free': 'Soy-free',
        'shellfish-allergy': 'Shellfish allergy',
        halal: 'Halal',
        kosher: 'Kosher',
      },
      cookingEffort: 'Cooking effort',
      cookingEffortOption: {
        'quick-weekdays': 'Quick weekdays',
        balanced: 'Balanced',
        'elaborate-weekends': 'Elaborate weekends',
      },
      weeklyGoals: 'Weekly Goals',
      minVeggie: 'Min. veggie',
      maxMeat: 'Max. meat',

      voice: 'Voice',
      voiceHint: '',
      voiceEnabled: 'Voice enabled',
      voiceSpeed: 'Voice speed',
      voiceSpeedOption: { slow: 'Slow', normal: 'Normal', fast: 'Fast' },
      voiceAutoRead: 'Auto-read',

      notifications: 'Notifications',
      notificationsHint: '',
      timerHaptic: 'Haptic',
      timerSound: 'Sound',
    },
  },
};

const mockProfile: UserProfile = {
  displayName: 'Test User',
  email: 'test@example.com',
  preferredLanguage: 'en',
  preferredUnitSystem: 'metric',
  defaultServings: 4,
  theme: 'system',
  voiceSettings: {
    enabled: true,
    speed: 'normal',
    autoReadInCookMode: false,
  },
  notificationPreferences: {
    timerHapticFeedback: true,
    timerSoundAlerts: true,
  },
  dietaryProfile: {
    dietaryType: 'vegetarian',
    restrictions: ['gluten-free'],
    minVeggieMeals: 3,
    maxRedMeatMeals: 2,
    cookingEffort: 'balanced',
  },
};

describe('ProfileSettingsComponent', () => {
  let fixture: ComponentFixture<ProfileSettingsComponent>;
  let apiMock: {
    getProfile: ReturnType<typeof vi.fn>;
    updateProfile: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    apiMock = {
      getProfile: vi.fn().mockReturnValue(of(mockProfile)),
      updateProfile: vi.fn().mockReturnValue(of(mockProfile)),
    };

    await TestBed.configureTestingModule({
      imports: [ProfileSettingsComponent, setupTranslocoTesting(en)],
      providers: [provideYumneyIcons(), { provide: UserProfileApiService, useValue: apiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileSettingsComponent);
    fixture.detectChanges();
  });

  it('should load profile on init', () => {
    expect(apiMock.getProfile).toHaveBeenCalled();
  });

  it('should render the form once the profile loads', () => {
    expect(fixture.nativeElement.querySelector('form')).toBeTruthy();
  });

  it('should populate the display name field from the profile', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#displayName');
    expect(input.value).toBe('Test User');
  });

  it('should render email as read-only', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#email');
    expect(input.value).toBe('test@example.com');
    expect(input.readOnly).toBe(true);
  });

  it('should populate servings from the profile', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#servings');
    expect(input.value).toBe('4');
  });

  it('should populate dietary type from the profile', () => {
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#dietaryType');
    expect(select.value).toBe('vegetarian');
  });

  it('should toggle a restriction on click', () => {
    fixture.componentInstance['onToggleRestriction']('nut-allergy');
    expect(fixture.componentInstance['restrictions']()).toContain('nut-allergy');

    fixture.componentInstance['onToggleRestriction']('nut-allergy');
    expect(fixture.componentInstance['restrictions']()).not.toContain('nut-allergy');
  });

  it('should debounce-save when a field changes', async () => {
    fixture.componentInstance['displayName'].set('Updated');
    fixture.componentInstance['onChange']();

    await new Promise((resolve) => setTimeout(resolve, 500));

    expect(apiMock.updateProfile).toHaveBeenCalledTimes(1);
    const request = apiMock.updateProfile.mock.calls[0][0];
    expect(request.displayName).toBe('Updated');
  });

  it('should show the error banner when load fails', () => {
    apiMock.getProfile.mockReturnValue(throwError(() => new Error('fail')));
    fixture.componentInstance['onRetry']();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Retry');
  });
});
