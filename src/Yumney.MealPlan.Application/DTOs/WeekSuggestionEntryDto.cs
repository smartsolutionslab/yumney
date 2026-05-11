namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record WeekSuggestionEntryDto(
	string Day,
	string MealType,
	Guid RecipeIdentifier,
	string RecipeTitle,
	string? FreshnessLabel,
	string? Reason);
