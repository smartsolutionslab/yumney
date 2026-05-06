using SmartSolutionsLab.Yumney.MealPlan.Api;
using SmartSolutionsLab.Yumney.MealPlan.Application;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;

var builder = WebApplication.CreateBuilder(args);

IModule[] modules =
[
	new MealPlanApiModule(),
	new MealPlanApplicationModule(),
	new MealPlanInfrastructureModule(),
];

builder.RegisterServices(modules);

var app = builder
	.Build()
	.RegisterEndpoints(modules);

app.Run();
