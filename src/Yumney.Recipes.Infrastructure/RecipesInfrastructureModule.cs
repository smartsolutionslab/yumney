using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure;

public sealed class RecipesInfrastructureModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddRecipesInfrastructure(builder.Configuration);
		return builder;
	}
}
