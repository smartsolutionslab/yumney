using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using Serilog;
using Yumney.Api;
using Yumney.Api.Middleware;
using Yumney.Recipes.Api;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Application.Interfaces;
using Yumney.Recipes.Domain.Recipe;
using Yumney.Recipes.Infrastructure.Persistence;
using Yumney.Recipes.Infrastructure.Services;
using Yumney.ServiceDefaults;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;
using Yumney.Shared.Events;
using Yumney.Shopping.Api;
using Yumney.Users.Api;
using Yumney.Users.Application.Commands;
using Yumney.Users.Application.Interfaces;
using Yumney.Users.Domain.AppUserProfile;
using Yumney.Users.Infrastructure;
using Yumney.Users.Infrastructure.Persistence;
using Yumney.Users.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.AddServiceDefaults();

var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new KeycloakOptions();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: keycloakOptions.Realm,
        configureOptions: options =>
        {
            options.RequireHttpsMetadata = false;
        });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

builder.Services.AddInProcessEventBus();

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("yumneydb"),
        x => x.MigrationsHistoryTable("__UsersMigrationsHistory")));
builder.Services.AddScoped<IAppUserProfileRepository, AppUserProfileRepository>();

builder.Services.AddDbContext<RecipesDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("yumneydb"),
        x => x.MigrationsHistoryTable("__RecipesMigrationsHistory")));
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<ImportRecipeRequestValidator>();
builder.Services.AddScoped<ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>>, ImportRecipeCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>, SaveRecipeCommandHandler>();

builder.Services.AddHttpClient<IWebScraper, WebScraper>().AddStandardResilienceHandler();
builder.Services.AddScoped<IRecipeExtractionService, SemanticKernelRecipeExtractionService>();

var skOptions = builder.Configuration.GetSection(SemanticKernelOptions.SectionName).Get<SemanticKernelOptions>() ?? new SemanticKernelOptions();

var kernelBuilder = builder.Services.AddKernel();

switch (skOptions.Provider)
{
    case SemanticKernelOptions.ProviderAzureOpenAI:
        kernelBuilder.AddAzureOpenAIChatCompletion(skOptions.ModelId, skOptions.Endpoint, skOptions.ApiKey);
        break;
    case SemanticKernelOptions.ProviderOllama:
        kernelBuilder.AddOpenAIChatCompletion(skOptions.ModelId, new Uri(skOptions.Endpoint), apiKey: null);
        break;
    default:
        kernelBuilder.AddOpenAIChatCompletion(skOptions.ModelId, skOptions.ApiKey);
        break;
}

builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
builder.Services.AddScoped<ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>, RegisterUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ResendVerificationEmailCommand, Result>, ResendVerificationEmailCommandHandler>();

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
