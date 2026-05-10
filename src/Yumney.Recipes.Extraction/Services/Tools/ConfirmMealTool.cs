using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing meal-state confirmation to the chat LLM. Used
/// for "I made the carbonara on Wednesday" / "skip Friday's planned dinner".
/// Cross-module via <see cref="IMealConfirmation"/>.
/// </summary>
/// <param name="confirmation">Consumer-defined confirmation contract.</param>
public sealed class ConfirmMealTool(IMealConfirmation confirmation)
{
	/// <summary>Mark a planned meal as cooked, skipped, or restored to planned.</summary>
	/// <param name="day">English weekday name (Monday..Sunday).</param>
	/// <param name="state">"Cooked", "Skipped", or "Planned".</param>
	/// <param name="mealType">Meal type. Defaults to Dinner.</param>
	/// <param name="year">ISO year, or 0 for the current year.</param>
	/// <param name="weekNumber">ISO week number 1-53, or 0 for the current week.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Human-readable confirmation string for the LLM to weave into its reply.</returns>
	[KernelFunction("confirm_meal_cooked")]
	[Description("Update a planned meal's state. Use for 'I made the spaghetti on Wednesday' (state=Cooked), 'skip Friday lunch' (state=Skipped), or 'I haven't cooked it yet' (state=Planned). Pass year=0 / weekNumber=0 to mean 'this week'.")]
	public async Task<string> ConfirmAsync(
		[Description("English weekday name: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday")] string day,
		[Description("Target state: Cooked, Skipped, or Planned")] string state,
		[Description("Meal type: Breakfast, Lunch, Dinner, or Snack. Defaults to Dinner.")] string mealType = "Dinner",
		[Description("ISO year, or 0 for the current year")] int year = 0,
		[Description("ISO week number 1-53, or 0 for the current week")] int weekNumber = 0,
		CancellationToken cancellationToken = default)
	{
		var (resolvedYear, resolvedWeek) = ResolveWeek(year, weekNumber);
		var request = new ConfirmMealRequest(resolvedYear, resolvedWeek, day, mealType, state);

		var success = await confirmation.ConfirmAsync(request, cancellationToken);
		if (!success) return $"Couldn't update {day}'s {mealType.ToLowerInvariant()} — the slot may not exist.";

		return $"Marked {day} {mealType.ToLowerInvariant()} as {state.ToLowerInvariant()}.";
	}

	private static (int Year, int Week) ResolveWeek(int year, int weekNumber)
	{
		var now = DateTimeOffset.UtcNow;
		var resolvedYear = year > 0 ? year : ISOWeek.GetYear(now.DateTime);
		var resolvedWeek = weekNumber > 0 ? weekNumber : ISOWeek.GetWeekOfYear(now.DateTime);
		return (resolvedYear, resolvedWeek);
	}
}
