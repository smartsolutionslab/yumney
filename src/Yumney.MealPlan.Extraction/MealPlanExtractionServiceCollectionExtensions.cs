using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Extraction.Services;
using SmartSolutionsLab.Yumney.MealPlan.Extraction.TestStubs;
using SmartSolutionsLab.Yumney.Shared.AI;

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

		services.AddKernelOnChatClient();

		return services;
	}
}
