using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.MealPlan.Client;

internal sealed class MealPlanClient(IModuleHttpClientFactory factory) : IMealPlanClient
{
	private readonly IModuleHttpClient http = factory.For("mealplan-api");

	public Task<WeeklyPlanResponse?> GetWeeklyPlanAsync(int year, int weekNumber, CancellationToken cancellationToken = default) =>
		http.FindAsync<WeeklyPlanResponse>(
			$"/api/v1/meal-plans/{year}/w/{weekNumber}",
			"GetWeeklyPlan",
			cancellationToken);
}
