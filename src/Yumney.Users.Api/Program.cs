using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shared.Modules;
using SmartSolutionsLab.Yumney.Users.Api;
using SmartSolutionsLab.Yumney.Users.Application;
using SmartSolutionsLab.Yumney.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

IModule[] modules =
[
	new UsersApiModule(),
	new UsersApplicationModule(),
	new UsersInfrastructureModule(),
];
builder.RegisterServices(modules);

var app = builder
	.Build()
	.RegisterEndpoints(modules);

app.Run();
