using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.MealPlan.Extraction;

public sealed class MealPlanExtractionModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddMealPlanExtraction(builder.Configuration);
		return builder;
	}
}
