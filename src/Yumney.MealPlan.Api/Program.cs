using SmartSolutionsLab.Yumney.MealPlan.Api;
using SmartSolutionsLab.Yumney.MealPlan.Application;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services
    .AddMealPlanApi()
    .AddMealPlanApplication()
    .AddMealPlanInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapMealPlanEndpoints();

app.Run();
