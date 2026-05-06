using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;
using SmartSolutionsLab.Yumney.Shopping.Api;
using SmartSolutionsLab.Yumney.Shopping.Application;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

IModule[] modules =
[
	new ShoppingApiModule(),
	new ShoppingApplicationModule(),
	new ShoppingInfrastructureModule(),
];
builder.RegisterServices(modules);

var app = builder
	.Build()
	.RegisterEndpoints(modules);

app.Run();
