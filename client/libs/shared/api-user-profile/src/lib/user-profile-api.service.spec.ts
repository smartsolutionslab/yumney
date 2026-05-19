import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserProfileApiService } from './user-profile-api.service';
import { API_ENDPOINTS } from '@yumney/shared/api-common';
import type { UserProfile, UpdateProfileRequest } from './user-profile';

const mockProfile: UserProfile = {
  displayName: 'Heiko',
  email: 'heiko@example.com',
  preferredLanguage: 'de',
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
    dietaryType: null,
    restrictions: [],
    minVeggieMeals: null,
    maxRedMeatMeals: null,
    cookingEffort: null,
  },
};

describe('UserProfileApiService', () => {
  let service: UserProfileApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(UserProfileApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  describe('getProfile', () => {
    it('should GET /api/v1/users/me/profile', () => {
      service.getProfile().subscribe((result) => {
        expect(result).toEqual(mockProfile);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.users.profile);
      expect(req.request.method).toBe('GET');
      req.flush(mockProfile);
    });
  });

  describe('updateProfile', () => {
    it('should PUT to /api/v1/users/me/profile', () => {
      const request: UpdateProfileRequest = {
        displayName: null,
        preferredLanguage: null,
        preferredUnitSystem: null,
        defaultServings: 2,
        theme: 'dark',
        voiceSettings: null,
        notificationPreferences: null,
        dietaryType: 'vegetarian',
        restrictions: ['gluten-free'],
        minVeggieMeals: 3,
        maxRedMeatMeals: null,
        cookingEffort: 'quick-weekdays',
      };

      const updatedProfile: UserProfile = {
        ...mockProfile,
        defaultServings: 2,
        theme: 'dark',
        dietaryProfile: {
          dietaryType: 'vegetarian',
          restrictions: ['gluten-free'],
          minVeggieMeals: 3,
          maxRedMeatMeals: null,
          cookingEffort: 'quick-weekdays',
        },
      };

      service.updateProfile(request).subscribe((result) => {
        expect(result).toEqual(updatedProfile);
      });

      const req = httpTesting.expectOne(API_ENDPOINTS.users.profile);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(updatedProfile);
    });
  });
});
