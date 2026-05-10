using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.MealPlan.Client;

public static class MealPlanClientServiceCollectionExtensions
{
	public static IServiceCollection AddMealPlanClient(this IServiceCollection services)
	{
		services.AddYumneyServiceClient("mealplan-api");
		services.AddSingleton<IMealPlanClient, MealPlanClient>();
		return services;
	}
}
