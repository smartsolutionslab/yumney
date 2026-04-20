import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ProfileSettingsComponent } from './profile-settings.component';
import { UserProfileApiService, type UserProfile } from '../api';
import { setupTranslocoTesting } from '@yumney/shared/models';

const en = {
  account: {
    settings: {
      title: 'Profile Settings',
      loading: 'Loading profile…',
      retry: 'Retry',
      household: 'Household',
      defaultServings: 'Default servings',
      dietary: 'Dietary Preferences',
      dietaryType: 'Dietary type',
      none: 'None',
      restrictions: 'Restrictions & allergies',
      cookingEffort: 'Cooking effort',
      weeklyGoals: 'Weekly Goals',
      minVeggie: 'Min. veggie meals',
      maxMeat: 'Max. red-meat meals',
      save: 'Save',
      saving: 'Saving…',
      saved: 'Saved!',
    },
  },
};

const mockProfile: UserProfile = {
  displayName: 'Test User',
  preferredLanguage: 'en',
  preferredUnitSystem: 'metric',
  defaultServings: 4,
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
      providers: [{ provide: UserProfileApiService, useValue: apiMock }],
    }).compileComponents();

    fixture = TestBed.createComponent(ProfileSettingsComponent);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should load profile on init', () => {
    expect(apiMock.getProfile).toHaveBeenCalled();
  });

  it('should render form after profile loads', () => {
    const form = fixture.nativeElement.querySelector('form');
    expect(form).toBeTruthy();
  });

  it('should display title', () => {
    expect(fixture.nativeElement.textContent).toContain('Profile Settings');
  });

  it('should populate servings from profile', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#servings');
    expect(input.value).toBe('4');
  });

  it('should populate dietary type from profile', () => {
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#dietaryType');
    expect(select.value).toBe('vegetarian');
  });

  it('should check active restrictions', () => {
    const checkboxes: NodeListOf<HTMLInputElement> = fixture.nativeElement.querySelectorAll(
      '.checkbox-label input[type="checkbox"]',
    );
    const checked = Array.from(checkboxes).filter((cb) => cb.checked);
    expect(checked.length).toBe(1);
  });

  it('should toggle restriction on click', () => {
    fixture.componentInstance['onToggleRestriction']('nut-allergy');
    expect(fixture.componentInstance['restrictions']()).toContain('nut-allergy');

    fixture.componentInstance['onToggleRestriction']('nut-allergy');
    expect(fixture.componentInstance['restrictions']()).not.toContain('nut-allergy');
  });

  it('should call updateProfile on save', () => {
    fixture.componentInstance['onSave']();

    expect(apiMock.updateProfile).toHaveBeenCalledWith({
      defaultServings: 4,
      dietaryType: 'vegetarian',
      restrictions: ['gluten-free'],
      minVeggieMeals: 3,
      maxRedMeatMeals: 2,
      cookingEffort: 'balanced',
    });
  });

  it('should show saved indicator after save', () => {
    fixture.componentInstance['onSave']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.saved-indicator')).toBeTruthy();
  });

  it('should show error with retry on load failure', () => {
    apiMock.getProfile.mockReturnValue(throwError(() => new Error('fail')));
    fixture.componentInstance['loadProfile']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('.retry-btn')).toBeTruthy();
  });

  it('should show error on save failure', () => {
    apiMock.updateProfile.mockReturnValue(throwError(() => new Error('fail')));
    fixture.componentInstance['onSave']();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error')).toBeTruthy();
  });
});
