using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Api;
using SmartSolutionsLab.Yumney.Shopping.Application;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services
    .AddShoppingApi()
    .AddShoppingApplication()
    .AddShoppingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapShoppingEndpoints();

app.Run();
