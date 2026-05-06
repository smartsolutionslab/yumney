using SmartSolutionsLab.Yumney.Recipes.Api;
using SmartSolutionsLab.Yumney.Recipes.Application;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

var builder = WebApplication.CreateBuilder(args);

IModule[] modules =
[
	new RecipesApiModule(),
	new RecipesApplicationModule(),
	new RecipesInfrastructureModule(),
	new RecipesExtractionModule(),
];
builder.RegisterServices(modules);

var app = builder
	.Build()
	.RegisterEndpoints(modules);

app.Run();
