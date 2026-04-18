import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { UserProfileApiService } from './user-profile-api.service';
import { API_ENDPOINTS } from './api-endpoints';
import type { UserProfile, UpdateProfileRequest } from './user-profile';

const mockProfile: UserProfile = {
  displayName: 'Heiko',
  preferredLanguage: 'de',
  preferredUnitSystem: 'metric',
  defaultServings: 4,
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
        defaultServings: 2,
        dietaryType: 'vegetarian',
        restrictions: ['gluten'],
        minVeggieMeals: 3,
        maxRedMeatMeals: null,
        cookingEffort: 'quick',
      };

      const updatedProfile: UserProfile = {
        ...mockProfile,
        defaultServings: 2,
        dietaryProfile: {
          dietaryType: 'vegetarian',
          restrictions: ['gluten'],
          minVeggieMeals: 3,
          maxRedMeatMeals: null,
          cookingEffort: 'quick',
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
