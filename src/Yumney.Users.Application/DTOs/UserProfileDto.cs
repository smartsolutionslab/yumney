namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record UserProfileDto(
	string DisplayName,
	string PreferredLanguage,
	string PreferredUnitSystem,
	int DefaultServings,
	DietaryProfileDto DietaryProfile);
