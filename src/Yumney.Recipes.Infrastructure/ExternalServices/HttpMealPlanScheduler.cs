using SmartSolutionsLab.Yumney.MealPlan.Client;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;

public sealed class HttpMealPlanScheduler(IMealPlanClient mealPlan) : IMealPlanScheduler
{
	public Task<bool> AssignAsync(AssignMealRequest request, CancellationToken cancellationToken = default)
	{
		var body = new AssignRecipeBody(
			request.Day,
			request.RecipeIdentifier,
			request.RecipeTitle,
			request.MealType,
			request.Servings);
		return mealPlan.AssignRecipeAsync(request.Year, request.WeekNumber, body, cancellationToken);
	}
}
