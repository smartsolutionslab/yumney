namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record UserProfileDto(
	string DisplayName,
	string Email,
	string PreferredLanguage,
	string PreferredUnitSystem,
	int DefaultServings,
	string Theme,
	VoiceSettingsDto VoiceSettings,
	NotificationPreferencesDto NotificationPreferences,
	DietaryProfileDto DietaryProfile);
