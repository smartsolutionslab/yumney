namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Consumer-defined contract for assigning recipes to slots in the user's
/// weekly meal plan. Owned by Recipes (the chat surface is the consumer)
/// per CLAUDE.md cross-module rule + ADR 0002.
/// </summary>
public interface IMealPlanScheduler
{
	/// <summary>Assign a recipe to a meal slot in the given week.</summary>
	/// <param name="request">Year, week, day, recipe, meal type, optional servings.</param>
	/// <param name="cancellationToken">Cancellation propagated to the call.</param>
	/// <returns>True if the assignment succeeded, false if the upstream rejected it.</returns>
	Task<bool> AssignAsync(AssignMealRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Consumer-flavored shape of an assign-meal request.</summary>
/// <param name="Year">ISO year.</param>
/// <param name="WeekNumber">ISO week number 1-53.</param>
/// <param name="Day">English weekday name (Monday..Sunday).</param>
/// <param name="MealType">One of "Breakfast", "Lunch", "Dinner", "Snack".</param>
/// <param name="RecipeIdentifier">Recipe identifier resolved from a prior search/get.</param>
/// <param name="RecipeTitle">Recipe title for downstream display.</param>
/// <param name="Servings">Optional override for slot servings; null = use recipe default.</param>
public sealed record AssignMealRequest(
	int Year,
	int WeekNumber,
	string Day,
	string MealType,
	Guid RecipeIdentifier,
	string RecipeTitle,
	int? Servings);
