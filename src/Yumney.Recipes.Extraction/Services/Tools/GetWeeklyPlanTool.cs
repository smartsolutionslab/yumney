using System.ComponentModel;
using System.Globalization;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// SK kernel function exposing the user's weekly meal plan to the chat LLM.
/// Cross-module call: <see cref="IWeeklyPlanLookup"/> is the consumer-defined
/// contract owned by Recipes; the HTTP adapter calls the MealPlan module via
/// <c>IMealPlanClient</c>.
/// </summary>
/// <param name="lookup">Consumer-defined weekly-plan lookup.</param>
/// <param name="context">Per-request collector for downstream suggestion / action emission.</param>
public sealed class GetWeeklyPlanTool(IWeeklyPlanLookup lookup, ChatToolContext context)
{
	/// <summary>Get the user's planned meals for an ISO week. Year + week default to "this week" when omitted.</summary>
	/// <param name="year">ISO year, e.g. 2026. Pass 0 for the current year.</param>
	/// <param name="weekNumber">ISO week number 1-53. Pass 0 for the current week.</param>
	/// <param name="cancellationToken">Cancellation propagated from the LLM call.</param>
	/// <returns>The weekly plan or null if nothing planned.</returns>
	[KernelFunction("get_weekly_plan")]
	[Description("Fetch the user's planned meals for an ISO week. Use for 'what's for dinner this week?', 'what did I plan for Tuesday?', 'show me next week's plan'. Pass year=0 and weekNumber=0 to mean 'this week'.")]
	public async Task<WeeklyPlanLookupResult?> GetAsync(
		[Description("ISO year (e.g. 2026), or 0 for the current year")] int year,
		[Description("ISO week number 1-53, or 0 for the current week")] int weekNumber,
		CancellationToken cancellationToken = default)
	{
		var (resolvedYear, resolvedWeek) = ResolveWeek(year, weekNumber);
		var plan = await lookup.GetForWeekAsync(resolvedYear, resolvedWeek, cancellationToken);
		if (plan is null) return null;

		foreach (var slot in plan.Slots)
		{
			if (slot.RecipeIdentifier is { } id && slot.RecipeTitle is { } title)
			{
				context.AppendRecipeMatch(id, title, $"{slot.Day} · {slot.MealType}");
			}
		}

		return plan;
	}

	private static (int Year, int Week) ResolveWeek(int year, int weekNumber)
	{
		var now = DateTimeOffset.UtcNow;
		var resolvedYear = year > 0 ? year : ISOWeek.GetYear(now.DateTime);
		var resolvedWeek = weekNumber > 0 ? weekNumber : ISOWeek.GetWeekOfYear(now.DateTime);
		return (resolvedYear, resolvedWeek);
	}
}
