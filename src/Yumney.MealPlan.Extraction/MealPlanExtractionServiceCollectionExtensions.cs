using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Extraction.Services;
using SmartSolutionsLab.Yumney.MealPlan.Extraction.TestStubs;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.MealPlan.Extraction;

public static class MealPlanExtractionServiceCollectionExtensions
{
	public static IServiceCollection AddMealPlanExtraction(this IServiceCollection services, IConfiguration configuration)
	{
		if (configuration.GetValue<bool>("E2ETests"))
		{
			services.AddSingleton<IWeekSuggestionService, StubWeekSuggestionService>();
			return services;
		}

		services.AddScoped<IWeekSuggestionService, SemanticKernelWeekSuggestionService>();

		var skOptions = configuration
			.GetSection(SemanticKernelOptions.SectionName)
			.Get<SemanticKernelOptions>() ?? new SemanticKernelOptions();

		var kernelBuilder = services.AddKernel();

		var (provider, modelId, endpoint, apiKey) = skOptions;

		switch (provider)
		{
			case SemanticKernelOptions.ProviderAzureOpenAI:
				kernelBuilder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
				break;
			case SemanticKernelOptions.ProviderOllama:
				var ollamaEndpoint = GetOllamaEndpoint(configuration, skOptions);
				if (ollamaEndpoint is not null)
				{
					kernelBuilder.AddOpenAIChatCompletion(modelId, new Uri(ollamaEndpoint), apiKey: null);
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

	private static string? GetOllamaEndpoint(IConfiguration configuration, SemanticKernelOptions skOptions) =>
		skOptions.Endpoint.HasValue() ? skOptions.Endpoint : configuration.GetConnectionString("ollama");
}
