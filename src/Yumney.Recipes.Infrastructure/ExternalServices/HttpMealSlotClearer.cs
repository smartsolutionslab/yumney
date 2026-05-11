using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpMealSlotClearer(IMealPlanClient mealPlan) : IMealSlotClearer
{
	public Task<bool> ClearAsync(ClearMealSlotRequest request, CancellationToken cancellationToken = default) =>
		mealPlan.ClearSlotAsync(
			request.Year,
			request.WeekNumber,
			new ClearSlotBody(request.Day, request.MealType),
			cancellationToken);
}
