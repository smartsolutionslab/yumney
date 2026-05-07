using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Shopping.Client;

public static class ShoppingClientServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingClient(this IServiceCollection services)
	{
		services.AddYumneyServiceClient("shopping-api");
		services.AddSingleton<IShoppingClient, ShoppingClient>();
		return services;
	}
}
