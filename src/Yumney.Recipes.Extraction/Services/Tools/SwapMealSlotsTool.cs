using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing meal-slot swapping to the chat LLM. Closes
/// the swap-via-chat half of US-329.
/// </summary>
/// <param name="swapper">Consumer-defined swapper contract.</param>
public sealed class SwapMealSlotsTool(IMealSlotSwapper swapper)
{
	/// <summary>Swap the meals planned for two days within the same ISO week.</summary>
	/// <param name="sourceDay">Source weekday name.</param>
	/// <param name="targetDay">Target weekday name.</param>
	/// <param name="mealType">Meal type. Defaults to Dinner.</param>
	/// <param name="year">ISO year, or 0 for the current year.</param>
	/// <param name="weekNumber">ISO week number 1-53, or 0 for the current week.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>Confirmation message for the LLM.</returns>
	[KernelFunction("swap_meal_slots")]
	[Description("Swap two meals in the user's weekly plan ('swap Thursday and Friday', 'move Monday's dinner to Tuesday'). Pass year=0 / weekNumber=0 to mean 'this week'.")]
	public async Task<string> SwapAsync(
		[Description("Source weekday name: Monday..Sunday")] string sourceDay,
		[Description("Target weekday name: Monday..Sunday")] string targetDay,
		[Description("Meal type: Breakfast, Lunch, Dinner, or Snack. Defaults to Dinner.")] string mealType = "Dinner",
		[Description("ISO year, or 0 for the current year")] int year = 0,
		[Description("ISO week number 1-53, or 0 for the current week")] int weekNumber = 0,
		CancellationToken cancellationToken = default)
	{
		var (resolvedYear, resolvedWeek) = ResolveWeek(year, weekNumber);
		var request = new SwapMealSlotsRequest(resolvedYear, resolvedWeek, sourceDay, targetDay, mealType);

		var success = await swapper.SwapAsync(request, cancellationToken);
		return success
			? $"Swapped {sourceDay} and {targetDay} {mealType.ToLowerInvariant()}."
			: $"Couldn't swap those meals — one or both slots may be empty.";
	}

	private static (int Year, int Week) ResolveWeek(int year, int weekNumber)
	{
		var now = DateTimeOffset.UtcNow;
		var resolvedYear = year > 0 ? year : ISOWeek.GetYear(now.DateTime);
		var resolvedWeek = weekNumber > 0 ? weekNumber : ISOWeek.GetWeekOfYear(now.DateTime);
		return (resolvedYear, resolvedWeek);
	}
}
