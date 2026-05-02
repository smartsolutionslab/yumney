using SmartSolutionsLab.Yumney.Recipes.Api;
using SmartSolutionsLab.Yumney.Recipes.Application;
using SmartSolutionsLab.Yumney.Recipes.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults(typeof(ShoppingListCreatedHandler).Assembly);

builder.Services
	.AddRecipesApi()
	.AddRecipesApplication()
	.AddRecipesInfrastructure(builder.Configuration)
	.AddRecipeExtraction(builder.Configuration)
	.AddCqrsLoggingDecorators();

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
	.RequireAuthorization()
	.MapRecipesEndpoints();

app.Run();
