using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Shared.Common;
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

		services.AddScoped<IShoppingItemCategorizer, SemanticKernelShoppingItemCategorizer>();

		var skOptions = configuration.GetSection(SemanticKernelOptions.SectionName).Get<SemanticKernelOptions>()
			?? new SemanticKernelOptions();
		var (provider, modelId, endpoint, apiKey) = skOptions;

		var kernelBuilder = services.AddKernel();
		switch (provider)
		{
			case SemanticKernelOptions.ProviderAzureOpenAI:
				kernelBuilder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
				break;
			case SemanticKernelOptions.ProviderOllama:
				var ollama = endpoint.HasValue() ? endpoint : configuration.GetConnectionString("ollama");
				if (ollama is not null)
				{
					kernelBuilder.AddOpenAIChatCompletion(modelId, new Uri(ollama), apiKey: null);
				}

				break;
			default:
				if (apiKey.HasValue())
				{
					kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
				}

				break;
		}

		return services;
	}
}
