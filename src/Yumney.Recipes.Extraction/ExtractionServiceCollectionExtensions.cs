using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.SemanticKernel;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class ExtractionServiceCollectionExtensions
{
	public static IServiceCollection AddRecipeExtraction(this IServiceCollection services, IConfiguration configuration)
	{
		services.TryAddSingleton<IExtractionResultCache, InMemoryExtractionResultCache>();

		if (configuration.GetValue<bool>("E2ETests"))
		{
			services.AddSingleton<IWebScraper, StubWebScraper>();
			services.AddSingleton<IRecipeExtractionService, StubRecipeExtractionService>();
			services.AddSingleton<IRecipeSuggestionService, StubRecipeSuggestionService>();
			services.AddSingleton<IIngredientRecognitionService, StubIngredientRecognitionService>();
			services.AddSingleton<IChatService, StubChatService>();
			services.AddSingleton<IIntentParserService, StubIntentParserService>();
			services.AddSingleton<IIngredientCategoryService, StubIngredientCategoryService>();
			return services;
		}

		services.AddHttpClient<IWebScraper, WebScraper>(BrowserHttpClientDefaults.ConfigureHttpClient)
			.ConfigurePrimaryHttpMessageHandler(BrowserHttpClientDefaults.CreateHandler)
			.AddStandardResilienceHandler();
		services.AddScoped<IRecipeExtractionService, SemanticKernelRecipeExtractionService>();
		services.AddScoped<IRecipeSuggestionService, SemanticKernelRecipeSuggestionService>();
		services.AddScoped<IIngredientRecognitionService, SemanticKernelIngredientRecognitionService>();
		services.AddScoped<IChatService, SemanticKernelChatService>();
		services.AddScoped<IIntentParserService, SemanticKernelIntentParserService>();
		services.AddScoped<IIngredientCategoryService, SemanticKernelIngredientCategoryService>();

		services.AddScoped<ChatToolContext>();
		services.AddScoped<SearchRecipesTool>();
		services.AddScoped<GetRecipeTool>();
		services.AddScoped<GetCookableRecipesTool>();

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

	private static string? GetOllamaEndpoint(IConfiguration configuration, SemanticKernelOptions skOptions)
	{
		return skOptions.Endpoint.HasValue() ? skOptions.Endpoint : configuration.GetConnectionString("ollama");
	}
}
