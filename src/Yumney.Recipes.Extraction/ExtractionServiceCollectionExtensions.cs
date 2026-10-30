using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Recipes.Extraction.TestStubs;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public static class ExtractionServiceCollectionExtensions
{
	public static IServiceCollection AddRecipeExtraction(this IServiceCollection services, IConfiguration configuration)
	{
		services.TryAddSingleton<IExtractionResultCache, DistributedExtractionResultCache>();

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
		services.AddScoped<IRecipeExtractionService, LlmRecipeExtractionService>();
		services.AddScoped<IRecipeSuggestionService, LlmRecipeSuggestionService>();
		services.AddScoped<IIngredientRecognitionService, LlmIngredientRecognitionService>();
		services.AddScoped<IChatService, LlmChatService>();
		services.AddScoped<IIntentParserService, LlmIntentParserService>();
		services.AddScoped<IIngredientCategoryService, LlmIngredientCategoryService>();

		services.AddScoped<ChatToolContext>();
		services.AddScoped<IChatTool, SearchRecipesTool>();
		services.AddScoped<IChatTool, GetRecipeTool>();
		services.AddScoped<IChatTool, GetCookableRecipesTool>();
		services.AddScoped<IChatTool, RateRecipeTool>();
		services.AddScoped<IChatTool, GetWeeklyPlanTool>();
		services.AddScoped<IChatTool, AssignMealTool>();
		services.AddScoped<IChatTool, ConfirmMealTool>();
		services.AddScoped<IChatTool, GetMergedShoppingListTool>();
		services.AddScoped<IChatTool, CreateShoppingListTool>();
		services.AddScoped<IChatTool, AddShoppingItemTool>();
		services.AddScoped<IChatTool, RemoveShoppingItemTool>();
		services.AddScoped<IChatTool, SwapMealSlotsTool>();
		services.AddScoped<IChatTool, ClearMealSlotTool>();

		return services;
	}
}
