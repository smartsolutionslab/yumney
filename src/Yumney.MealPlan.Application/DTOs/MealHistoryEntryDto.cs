namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record MealHistoryEntryDto(
	Guid? RecipeIdentifier,
	string RecipeTitle,
	string Week,
	string Day,
	string MealType);
