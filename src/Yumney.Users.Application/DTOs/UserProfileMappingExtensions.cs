using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public static class UserProfileMappingExtensions
{
	public static UserProfileDto ToDto(this AppUserProfile profile, string email) =>
		new(
			profile.DisplayName.Value,
			email,
			profile.PreferredLanguage.Value,
			profile.PreferredUnitSystem.Value,
			profile.DefaultServings.Value,
			profile.Theme.Value,
			profile.VoiceSettings.ToDto(),
			profile.NotificationPreferences.ToDto(),
			profile.DietaryProfile.ToDto());

	public static DietaryProfileDto ToDto(this DietaryProfile dietary) =>
		new(
			dietary.DietaryType?.Value,
			dietary.Restrictions.Select(restriction => restriction.Value).ToList(),
			dietary.BalanceGoals.MinVeggieMeals,
			dietary.BalanceGoals.MaxRedMeatMeals,
			dietary.CookingEffort?.Value);

	public static VoiceSettingsDto ToDto(this VoiceSettings voiceSettings) =>
		new(voiceSettings.Enabled, voiceSettings.Speed.Value, voiceSettings.AutoReadInCookMode);

	public static NotificationPreferencesDto ToDto(this NotificationPreferences preferences) =>
		new(preferences.TimerHapticFeedback, preferences.TimerSoundAlerts);
}
