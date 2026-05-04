using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record UpdateUserProfileCommand(
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
	string? CookingEffort) : ICommand<Result<UserProfileDto>>;
