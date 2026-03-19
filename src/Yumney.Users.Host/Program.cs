using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Api;
using SmartSolutionsLab.Yumney.Users.Application;
using SmartSolutionsLab.Yumney.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services.AddUsersApplication();
builder.Services.AddUsersInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapUsersEndpoints();

app.Run();
