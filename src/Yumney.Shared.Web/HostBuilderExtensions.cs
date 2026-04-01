using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using RedisRateLimiting;
using Scalar.AspNetCore;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.MassTransit;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using SmartSolutionsLab.Yumney.Shared.Web.Services;
using StackExchange.Redis;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class HostBuilderExtensions
{
    public static WebApplicationBuilder AddYumneyDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();
        builder.AddRedisDistributedCache("redis");
        builder.AddRedisClient("redis");

        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = KeycloakDefaults.RealmUrl(builder.Configuration);
                options.Audience = KeycloakDefaults.Audience;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.ValidateIssuer = false;
            });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUserProvider>();

        // Domain events: dispatched in-process (same transaction boundary)
        // Integration events: published via MassTransit/RabbitMQ (cross-instance)
        // MassTransit registration overrides InProcessEventBus for IEventBus (last-wins in DI)
        builder.Services.AddInProcessEventBus();
        builder.Services.AddMassTransitEventBus(builder.Configuration);

        builder.Services.AddScoped<DomainEventDispatchInterceptor>();

        var configuration = builder.Configuration;

        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                var components = document.Components ?? new OpenApiComponents();
                document.Components = components;
                components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                components.SecuritySchemes["keycloak"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(KeycloakDefaults.AuthorizationUrl(configuration)),
                            TokenUrl = new Uri(KeycloakDefaults.TokenUrl(configuration)),
                            Scopes = new Dictionary<string, string>
                            {
                                [KeycloakDefaults.ScopeOpenId] = "OpenID Connect",
                                [KeycloakDefaults.ScopeProfile] = "User profile",
                            },
                        },
                    },
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("keycloak", document)] = [KeycloakDefaults.ScopeOpenId, KeycloakDefaults.ScopeProfile],
                });

                return Task.CompletedTask;
            });
        });

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
        builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.RecipeImport, context =>
            {
                var userId = context.User?.FindFirstValue(KeycloakClaimTypes.Subject) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RedisRateLimitPartition.GetSlidingWindowRateLimiter(userId, _ => new RedisSlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
                });
            });

            options.AddPolicy(RateLimitPolicies.GeneralApi, context =>
            {
                var userId = context.User?.FindFirstValue(KeycloakClaimTypes.Subject) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RedisRateLimitPartition.GetFixedWindowRateLimiter(userId, _ => new RedisFixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
                });
            });
        });

        return builder;
    }

    public static WebApplication UseYumneyDefaults(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseResponseCompression();

        app.UseAuthentication()
            .UseAuthorization();

        app.UseRateLimiter();

        app.MapOpenApi();

        if (!app.Environment.IsProduction())
        {
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Yumney API")
                    .WithTheme(ScalarTheme.Saturn)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .AddAuthorizationCodeFlow("keycloak", flow => flow
                        .WithClientId(KeycloakDefaults.WebClientId)
                        .WithAuthorizationUrl(KeycloakDefaults.AuthorizationUrl(app.Configuration))
                        .WithTokenUrl(KeycloakDefaults.TokenUrl(app.Configuration))
                        .WithSelectedScopes([KeycloakDefaults.ScopeOpenId, KeycloakDefaults.ScopeProfile]));
            });
        }

        app.MapDefaultEndpoints();

        app.MapGet("/version", () => new
        {
            Version = typeof(HostBuilderExtensions).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            Environment = app.Environment.EnvironmentName,
        }).AllowAnonymous();

        return app;
    }
}

/// <summary>Rate limit policy names.</summary>
public static class RateLimitPolicies
{
    /// <summary>Strict limit for LLM recipe import (10/min).</summary>
    public const string RecipeImport = "RecipeImport";

    /// <summary>General API rate limit (60/min).</summary>
    public const string GeneralApi = "GeneralApi";
}
