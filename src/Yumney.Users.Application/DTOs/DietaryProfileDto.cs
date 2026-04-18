namespace SmartSolutionsLab.Yumney.Users.Application.DTOs;

public sealed record DietaryProfileDto(
	string? DietaryType,
	IReadOnlyList<string> Restrictions,
	int? MinVeggieMeals,
	int? MaxRedMeatMeals,
	string? CookingEffort);
