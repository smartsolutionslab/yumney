using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.MealPlan.Application;

public sealed class MealPlanApplicationModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddMealPlanApplication();
		return builder;
	}
}
