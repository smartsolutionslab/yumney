using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Extraction.Services;

namespace SmartSolutionsLab.Yumney.Shopping.Extraction;

public static class ShoppingExtractionServiceCollectionExtensions
{
	public static IServiceCollection AddShoppingExtraction(this IServiceCollection services, IConfiguration configuration)
	{
		if (configuration.GetValue<bool>("E2ETests"))
		{
			services.AddSingleton<IShoppingItemCategorizer, StubShoppingItemCategorizer>();
			return services;
		}

		services.AddScoped<IShoppingItemCategorizer, LlmShoppingItemCategorizer>();

		return services;
	}
}
