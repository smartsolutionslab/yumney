using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpWeeklyPlanLookup(IMealPlanClient mealPlan) : IWeeklyPlanLookup
{
	public async Task<WeeklyPlanLookupResult?> GetForWeekAsync(int year, int weekNumber, CancellationToken cancellationToken = default)
	{
		var response = await mealPlan.GetWeeklyPlanAsync(year, weekNumber, cancellationToken);
		return response?.ToLookupResult();
	}
}
