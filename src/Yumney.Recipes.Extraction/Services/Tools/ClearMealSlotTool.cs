using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing meal-slot clearing to the chat LLM. Closes
/// the cancel-via-chat half of US-329.
/// </summary>
/// <param name="clearer">Consumer-defined clearer contract.</param>
public sealed class ClearMealSlotTool(IMealSlotClearer clearer)
{
	/// <summary>Clear (cancel) the meal planned for a given day + meal type.</summary>
	/// <param name="day">Weekday name to clear.</param>
	/// <param name="mealType">Meal type. Defaults to Dinner.</param>
	/// <param name="year">ISO year, or 0 for the current year.</param>
	/// <param name="weekNumber">ISO week number 1-53, or 0 for the current week.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Confirmation message for the LLM.</returns>
	[KernelFunction("clear_meal_slot")]
	[Description("Cancel / clear a planned meal ('cancel Wednesday', 'remove Friday's dinner', 'clear lunch on Monday'). Pass year=0 / weekNumber=0 to mean 'this week'.")]
	public async Task<string> ClearAsync(
		[Description("Weekday name to clear: Monday..Sunday")] string day,
		[Description("Meal type: Breakfast, Lunch, Dinner, or Snack. Defaults to Dinner.")] string mealType = "Dinner",
		[Description("ISO year, or 0 for the current year")] int year = 0,
		[Description("ISO week number 1-53, or 0 for the current week")] int weekNumber = 0,
		CancellationToken cancellationToken = default)
	{
		var (resolvedYear, resolvedWeek) = ResolveWeek(year, weekNumber);
		var request = new ClearMealSlotRequest(resolvedYear, resolvedWeek, day, mealType);

		var success = await clearer.ClearAsync(request, cancellationToken);
		return success
			? $"Cleared {day} {mealType.ToLowerInvariant()}."
			: $"Couldn't clear that slot — it may already be empty.";
	}

	private static (int Year, int Week) ResolveWeek(int year, int weekNumber)
	{
		var now = DateTimeOffset.UtcNow;
		var resolvedYear = year > 0 ? year : ISOWeek.GetYear(now.DateTime);
		var resolvedWeek = weekNumber > 0 ? weekNumber : ISOWeek.GetWeekOfYear(now.DateTime);
		return (resolvedYear, resolvedWeek);
	}
}
