using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Client;

public static class RecipesClientServiceCollectionExtensions
{
	public static IServiceCollection AddRecipesClient(this IServiceCollection services)
	{
		services.AddYumneyServiceClient("recipes-api");
		services.AddSingleton<IRecipesClient, RecipesClient>();
		return services;
	}
}
