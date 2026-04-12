export interface DietaryProfileDto {
  dietaryType: string | null;
  restrictions: string[];
  minVeggieMeals: number | null;
  maxRedMeatMeals: number | null;
  cookingEffort: string | null;
}

export interface UserProfile {
  displayName: string;
  preferredLanguage: string;
  preferredUnitSystem: string;
  defaultServings: number;
  dietaryProfile: DietaryProfileDto;
}

export interface UpdateProfileRequest {
  defaultServings: number;
  dietaryType: string | null;
  restrictions: string[];
  minVeggieMeals: number | null;
  maxRedMeatMeals: number | null;
  cookingEffort: string | null;
}
