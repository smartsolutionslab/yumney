using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.SemanticKernel;
using Scalar.AspNetCore;
using Serilog;
using SmartSolutionsLab.Yumney.Api;
using SmartSolutionsLab.Yumney.Api.Middleware;
using SmartSolutionsLab.Yumney.Recipes.Api;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Services;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shopping.Api;
using SmartSolutionsLab.Yumney.Users.Api;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

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
builder.Services.AddScoped<IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>, GetRecipesQueryHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>>, UpdateRecipeCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteRecipeCommand, Result>, DeleteRecipeCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>, GetRecipeByIdQueryHandler>();

builder.Services.Configure<ScrapingOptions>(builder.Configuration.GetSection(ScrapingOptions.SectionName));
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("RecipeImport", httpContext =>
    {
        var userId = httpContext.User?.FindFirst("sub")?.Value ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.UseSerilogRequestLogging()
    .UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication()
    .UseAuthorization();

app.UseRateLimiter();

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
