using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Api;
using SmartSolutionsLab.Yumney.Users.Application;
using SmartSolutionsLab.Yumney.Users.Application.IntegrationEventHandlers;
using SmartSolutionsLab.Yumney.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults(typeof(RecipeImportedActivityHandler).Assembly);

builder.Services.AddUsersApi();
builder.Services.AddUsersApplication();
builder.Services.AddUsersInfrastructure(builder.Configuration);
builder.Services.AddCqrsLoggingDecorators();

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
	.RequireAuthorization()
	.MapUsersEndpoints();

app.Run();
