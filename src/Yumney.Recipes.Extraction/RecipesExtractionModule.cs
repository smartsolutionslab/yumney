using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction;

public sealed class RecipesExtractionModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.AddYumneyChatClient();
		builder.Services.AddRecipeExtraction(builder.Configuration);
		return builder;
	}
}
