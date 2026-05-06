using SmartSolutionsLab.Yumney.Recipes.Api;
using SmartSolutionsLab.Yumney.Recipes.Application;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Hosting;

ModuleHost.Run(
	args,
	new RecipesApiModule(),
	new RecipesApplicationModule(),
	new RecipesInfrastructureModule(),
	new RecipesExtractionModule());
