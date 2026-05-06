using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure;

public sealed class MealPlanInfrastructureModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddMealPlanInfrastructure(builder.Configuration);
		return builder;
	}
}
