namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record WeeklyPlannedRecipesDto(
	string Week,
	IReadOnlyList<PlannedRecipeDto> Recipes);
