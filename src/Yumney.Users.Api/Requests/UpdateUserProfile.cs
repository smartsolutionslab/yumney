using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Api.Requests;

public sealed record UpdateUserProfile(
	string? DisplayName,
	string? PreferredLanguage,
	string? PreferredUnitSystem,
	int DefaultServings,
	string? Theme,
	VoiceSettingsDto? VoiceSettings,
	NotificationPreferencesDto? NotificationPreferences,
	string? DietaryType,
	IReadOnlyList<string> Restrictions,
	int? MinVeggieMeals,
	int? MaxRedMeatMeals,
	string? CookingEffort)
{
	public void Deconstruct(
		out DisplayName? displayName,
		out PreferredLanguage? preferredLanguage,
		out PreferredUnitSystem? preferredUnitSystem,
		out DefaultServings defaultServings,
		out Theme? theme,
		out VoiceSettings? voiceSettings,
		out NotificationPreferences? notificationPreferences,
		out DietaryProfile dietaryProfile)
	{
		displayName = DisplayName is null ? null : Domain.AppUserProfile.DisplayName.From(DisplayName);
		preferredLanguage = PreferredLanguage is null ? null : Domain.AppUserProfile.PreferredLanguage.From(PreferredLanguage);
		preferredUnitSystem = PreferredUnitSystem is null ? null : Domain.AppUserProfile.PreferredUnitSystem.From(PreferredUnitSystem);
		defaultServings = Domain.AppUserProfile.DefaultServings.From(DefaultServings);
		theme = Theme is null ? null : Domain.AppUserProfile.Theme.From(Theme);
		voiceSettings = VoiceSettings is null
			? null
			: new VoiceSettings(VoiceSettings.Enabled, VoiceSpeed.From(VoiceSettings.Speed), VoiceSettings.AutoReadInCookMode);
		notificationPreferences = NotificationPreferences is null
			? null
			: new NotificationPreferences(NotificationPreferences.TimerHapticFeedback, NotificationPreferences.TimerSoundAlerts);
		dietaryProfile = Domain.AppUserProfile.DietaryProfile.From(
			DietaryType is null ? null : Domain.AppUserProfile.DietaryType.From(DietaryType),
			Restrictions.Select(DietaryRestriction.From).ToList(),
			WeeklyBalanceGoals.From(MinVeggieMeals, MaxRedMeatMeals),
			CookingEffort is null ? null : CookingEffortPreference.From(CookingEffort));
	}
}
