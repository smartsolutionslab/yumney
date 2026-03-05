using Scalar.AspNetCore;
using Serilog;
using Yumney.Api.Middleware;
using Yumney.Recipes.Api;
using Yumney.ServiceDefaults;
using Yumney.Shared.Common;
using Yumney.Shared.Events;
using Yumney.Shopping.Api;
using Yumney.Users.Api;
using Yumney.Users.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.AddServiceDefaults();

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "yumney",
        configureOptions: options =>
        {
            options.RequireHttpsMetadata = false;
        });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.AddInProcessEventBus();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

var api = app.MapGroup("/api/v1")
    .RequireAuthorization();

api.MapRecipesEndpoints();
api.MapShoppingEndpoints();
api.MapUsersEndpoints();

app.Run();
