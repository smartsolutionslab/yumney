using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpMealSlotSwapper(IMealPlanClient mealPlan) : IMealSlotSwapper
{
	public Task<bool> SwapAsync(SwapMealSlotsRequest request, CancellationToken cancellationToken = default) =>
		mealPlan.SwapSlotsAsync(
			request.Year,
			request.WeekNumber,
			new SwapSlotsBody(request.SourceDay, request.TargetDay, request.MealType),
			cancellationToken);
}
