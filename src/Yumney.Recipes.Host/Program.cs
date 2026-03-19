using SmartSolutionsLab.Yumney.Recipes.Api;
using SmartSolutionsLab.Yumney.Recipes.Application;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services.AddRecipesApplication();
builder.Services.AddRecipesInfrastructure(builder.Configuration);
builder.Services.AddRecipeExtraction(builder.Configuration);

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapRecipesEndpoints();

app.Run();
