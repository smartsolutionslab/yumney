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

	public async Task<bool> AssignRecipeAsync(int year, int weekNumber, AssignRecipeBody body, CancellationToken cancellationToken = default)
	{
		try
		{
			await http.PostAsync(
				$"/api/v1/meal-plans/{year}/w/{weekNumber}/slots",
				body,
				"AssignRecipe",
				cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}

	public async Task<bool> ConfirmMealAsync(int year, int weekNumber, ConfirmMealBody body, CancellationToken cancellationToken = default)
	{
		try
		{
			await http.PutAsync(
				$"/api/v1/meal-plans/{year}/w/{weekNumber}/slots/confirm",
				body,
				"ConfirmMeal",
				cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}

	public async Task<bool> SwapSlotsAsync(int year, int weekNumber, SwapSlotsBody body, CancellationToken cancellationToken = default)
	{
		try
		{
			await http.PutAsync(
				$"/api/v1/meal-plans/{year}/w/{weekNumber}/slots/swap",
				body,
				"SwapMealSlots",
				cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}

	public async Task<bool> ClearSlotAsync(int year, int weekNumber, ClearSlotBody body, CancellationToken cancellationToken = default)
	{
		try
		{
			await http.DeleteAsync(
				$"/api/v1/meal-plans/{year}/w/{weekNumber}/slots",
				body,
				"ClearMealSlot",
				cancellationToken);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			return false;
		}
	}
}
