export interface DietaryProfileDto {
  dietaryType: string | null;
  restrictions: string[];
  minVeggieMeals: number | null;
  maxRedMeatMeals: number | null;
  cookingEffort: string | null;
}

export interface VoiceSettingsDto {
  enabled: boolean;
  speed: 'slow' | 'normal' | 'fast';
  autoReadInCookMode: boolean;
}

export interface NotificationPreferencesDto {
  timerHapticFeedback: boolean;
  timerSoundAlerts: boolean;
}

export type ThemePreference = 'light' | 'dark' | 'system';

export interface UserProfile {
  displayName: string;
  email: string;
  preferredLanguage: string;
  preferredUnitSystem: string;
  defaultServings: number;
  theme: ThemePreference;
  voiceSettings: VoiceSettingsDto;
  notificationPreferences: NotificationPreferencesDto;
  dietaryProfile: DietaryProfileDto;
}

export interface UpdateProfileRequest {
  displayName: string | null;
  preferredLanguage: string | null;
  preferredUnitSystem: string | null;
  defaultServings: number;
  theme: ThemePreference | null;
  voiceSettings: VoiceSettingsDto | null;
  notificationPreferences: NotificationPreferencesDto | null;
  dietaryType: string | null;
  restrictions: string[];
  minVeggieMeals: number | null;
  maxRedMeatMeals: number | null;
  cookingEffort: string | null;
}
