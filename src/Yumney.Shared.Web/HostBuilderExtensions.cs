using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
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

        var realm = builder.Configuration.GetValue<string>(KeycloakDefaults.RealmConfigKey) ?? KeycloakDefaults.DefaultRealm;

        var keycloakUrl = builder.Configuration.GetConnectionString(KeycloakDefaults.ConnectionStringName)
            ?? KeycloakDefaults.DefaultUrl;

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{keycloakUrl}/realms/{realm}";
                options.Audience = KeycloakDefaults.Audience;
                options.RequireHttpsMetadata = false;
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

        var authorizationUrl = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/auth";
        var tokenUrl = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";

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
                            AuthorizationUrl = new Uri(authorizationUrl),
                            TokenUrl = new Uri(tokenUrl),
                            Scopes = new Dictionary<string, string>
                            {
                                ["openid"] = "OpenID Connect",
                                ["profile"] = "User profile",
                            },
                        },
                    },
                };

                document.Security ??= [];
                document.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("keycloak", document)] = ["openid", "profile"],
                });

                return Task.CompletedTask;
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.RecipeImport, context =>
            {
                var userId = context.User?.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                return RedisRateLimitPartition.GetSlidingWindowRateLimiter(userId, _ => new RedisSlidingWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
                });
            });

            options.AddPolicy(RateLimitPolicies.GeneralApi, context =>
            {
                var userId = context.User?.FindFirstValue("sub") ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
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

        app.UseAuthentication()
            .UseAuthorization();

        app.UseRateLimiter();

        app.MapOpenApi();

        if (!app.Environment.IsProduction())
        {
            var keycloakUrl = app.Configuration.GetConnectionString(KeycloakDefaults.ConnectionStringName)
                ?? KeycloakDefaults.DefaultUrl;
            var realm = app.Configuration.GetValue<string>(KeycloakDefaults.RealmConfigKey)
                ?? KeycloakDefaults.DefaultRealm;

            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("Yumney API")
                    .WithTheme(ScalarTheme.Saturn)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .AddAuthorizationCodeFlow("keycloak", flow => flow
                        .WithClientId(KeycloakDefaults.Audience)
                        .WithAuthorizationUrl($"{keycloakUrl}/realms/{realm}/protocol/openid-connect/auth")
                        .WithTokenUrl($"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token")
                        .WithSelectedScopes(["openid", "profile"]));
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
