using SmartSolutionsLab.Yumney.MealPlan.Api;
using SmartSolutionsLab.Yumney.MealPlan.Application;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults(typeof(MealPlanInfrastructureServiceCollectionExtensions).Assembly);

builder.Services
	.AddMealPlanApi()
	.AddMealPlanApplication()
	.AddMealPlanInfrastructure(builder.Configuration)
	.AddCqrsLoggingDecorators();

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
	.RequireAuthorization()
	.MapMealPlanEndpoints();

app.Run();
