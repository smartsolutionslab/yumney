using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Scalar.AspNetCore;
using Serilog;
using Yumney.Api.Middleware;
using Yumney.Recipes.Api;
using Yumney.ServiceDefaults;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;
using Yumney.Shared.Events;
using Yumney.Shopping.Api;
using Yumney.Users.Api;
using Yumney.Users.Application.Commands;
using Yumney.Users.Application.Interfaces;
using Yumney.Users.Infrastructure;
using Yumney.Users.Infrastructure.Persistence;
using Yumney.Users.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

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

builder.Services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("yumneydb")));
builder.Services.AddScoped<IAppUserProfileRepository, AppUserProfileRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();
builder.Services.AddScoped<ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>, RegisterUserCommandHandler>();

builder.Services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client => { client.BaseAddress = new Uri("https+http://keycloak"); })
.AddStandardResilienceHandler();

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.UseSerilogRequestLogging()
    .UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication()
    .UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

var api = app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapRecipesEndpoints()
    .MapShoppingEndpoints()
    .MapUsersEndpoints();

app.Run();
