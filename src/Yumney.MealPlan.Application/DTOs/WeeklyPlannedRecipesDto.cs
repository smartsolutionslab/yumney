namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

/// <summary>
/// All recipes planned for a week — input for shopping list generation.
/// </summary>
public sealed record WeeklyPlannedRecipesDto(
	string Week,
	IReadOnlyList<PlannedRecipeDto> Recipes);
