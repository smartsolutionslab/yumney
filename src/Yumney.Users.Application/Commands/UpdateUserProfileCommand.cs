using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

namespace SmartSolutionsLab.Yumney.Users.Application.Commands;

public sealed record UpdateUserProfileCommand(
	DisplayName? DisplayName,
	PreferredLanguage? PreferredLanguage,
	PreferredUnitSystem? PreferredUnitSystem,
	DefaultServings DefaultServings,
	Theme? Theme,
	VoiceSettings? VoiceSettings,
	NotificationPreferences? NotificationPreferences,
	DietaryProfile DietaryProfile) : ICommand<Result<UserProfileDto>>;
