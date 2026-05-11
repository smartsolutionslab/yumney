namespace SmartSolutionsLab.Yumney.MealPlan.Application.DTOs;

public sealed record MealAnalyticsDto(
	string Period,
	int TotalCooked,
	int TotalSkipped,
	int UniqueRecipes,
	decimal MealsPerWeek,
	int DiscoveryRate,
	IReadOnlyList<TopRecipeDto> TopRecipes,
	IReadOnlyList<CategoryShareDto> CategoryDistribution);
