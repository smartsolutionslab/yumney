using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpMealConfirmation(IMealPlanClient mealPlan) : IMealConfirmation
{
	public Task<bool> ConfirmAsync(ConfirmMealRequest request, CancellationToken cancellationToken = default)
	{
		var body = new ConfirmMealBody(request.Day, request.MealType, request.State);
		return mealPlan.ConfirmMealAsync(request.Year, request.WeekNumber, body, cancellationToken);
	}
}
