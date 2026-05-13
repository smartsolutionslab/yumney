using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Recipes.Application;

public sealed class RecipesApplicationModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddRecipesApplication();
		return builder;
	}
}
