using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing meal assignment to the chat LLM. Cross-module:
/// <see cref="IMealPlanScheduler"/> is the consumer-defined contract owned by
/// Recipes; the HTTP adapter calls MealPlan via <c>IMealPlanClient</c>.
/// </summary>
/// <param name="scheduler">Consumer-defined scheduler.</param>
/// <param name="context">Per-request collector for downstream suggestion / action emission.</param>
public sealed class AssignMealTool(IMealPlanScheduler scheduler, ChatToolContext context)
{
	/// <summary>Plan a recipe into a meal slot for an ISO week.</summary>
	/// <param name="day">English weekday name (Monday..Sunday).</param>
	/// <param name="recipeIdentifier">Recipe GUID resolved from a prior search/get.</param>
	/// <param name="recipeTitle">Recipe title for downstream display.</param>
	/// <param name="mealType">Meal type. Defaults to Dinner.</param>
	/// <param name="year">ISO year, or 0 for the current year.</param>
	/// <param name="weekNumber">ISO week number 1-53, or 0 for the current week.</param>
	/// <param name="servings">Optional override for slot servings; null = use recipe default.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Human-readable confirmation string for the LLM to weave into its reply.</returns>
	[KernelFunction("assign_meal")]
	[Description("Plan a recipe into a meal slot ('plan carbonara for Wednesday', 'add risotto to Friday lunch'). Pass year=0 / weekNumber=0 to mean 'this week'. Requires a recipe identifier from a previous search_recipes / get_recipe call.")]
	public async Task<string> AssignAsync(
		[Description("English weekday name: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday")] string day,
		[Description("Recipe identifier (GUID) returned by search_recipes or get_recipe")] string recipeIdentifier,
		[Description("Recipe title for display")] string recipeTitle,
		[Description("Meal type: Breakfast, Lunch, Dinner, or Snack. Defaults to Dinner.")] string mealType = "Dinner",
		[Description("ISO year, or 0 for the current year")] int year = 0,
		[Description("ISO week number 1-53, or 0 for the current week")] int weekNumber = 0,
		[Description("Optional override for slot servings; omit to use the recipe default")] int? servings = null,
		CancellationToken cancellationToken = default)
	{
		if (!Guid.TryParse(recipeIdentifier, out var recipe)) return "Invalid recipe identifier — call search_recipes first.";

		var (resolvedYear, resolvedWeek) = ResolveWeek(year, weekNumber);
		var request = new AssignMealRequest(
			resolvedYear,
			resolvedWeek,
			day,
			mealType,
			recipe,
			recipeTitle,
			servings);

		var success = await scheduler.AssignAsync(request, cancellationToken);
		if (!success) return $"Couldn't plan {recipeTitle} for {day} — please try again.";

		context.AppendRecipeMatch(recipe, recipeTitle, $"Planned for {day} · {mealType}");
		return $"Planned {recipeTitle} for {day} ({mealType}).";
	}

	private static (int Year, int Week) ResolveWeek(int year, int weekNumber)
	{
		var now = DateTimeOffset.UtcNow;
		var resolvedYear = year > 0 ? year : ISOWeek.GetYear(now.DateTime);
		var resolvedWeek = weekNumber > 0 ? weekNumber : ISOWeek.GetWeekOfYear(now.DateTime);
		return (resolvedYear, resolvedWeek);
	}
}
