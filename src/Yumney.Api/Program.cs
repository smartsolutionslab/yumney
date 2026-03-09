using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using Serilog;
using Yumney.Api.Middleware;
using Yumney.Recipes.Api;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Application.DTOs;
using Yumney.Recipes.Application.Interfaces;
using Yumney.Recipes.Infrastructure.Services;
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

builder.Services.AddValidatorsFromAssemblyContaining<ImportRecipeRequestValidator>();
builder.Services.AddScoped<ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>>, ImportRecipeCommandHandler>();

builder.Services.AddHttpClient<IWebScraper, WebScraper>()
    .AddStandardResilienceHandler();
builder.Services.AddScoped<IRecipeExtractionService, SemanticKernelRecipeExtractionService>();

var skConfig = builder.Configuration.GetSection("SemanticKernel");
var kernelBuilder = builder.Services.AddKernel();

var provider = skConfig["Provider"] ?? "OpenAI";
var modelId = skConfig["ModelId"] ?? "gpt-4o-mini";
var apiKey = skConfig["ApiKey"] ?? string.Empty;

switch (provider)
{
    case "AzureOpenAI":
        var endpoint = skConfig["Endpoint"] ?? string.Empty;
        kernelBuilder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
        break;
    case "Ollama":
        var ollamaEndpoint = new Uri(skConfig["Endpoint"] ?? "http://localhost:11434/v1");
        kernelBuilder.AddOpenAIChatCompletion(modelId, ollamaEndpoint, apiKey: null);
        break;
    default:
        kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey);
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
