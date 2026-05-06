using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public sealed class RecipesExtractionModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.Services.AddRecipeExtraction(builder.Configuration);
		return builder;
	}
}
