using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using RedisRateLimiting;
using Scalar.AspNetCore;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using SmartSolutionsLab.Yumney.Shared.Web.Services;
using StackExchange.Redis;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class HostBuilderExtensions
{
	public static WebApplicationBuilder AddYumneyDefaults(this WebApplicationBuilder builder, params Assembly[] eventHandlerAssemblies)
	{
		builder.AddServiceDefaults();
		builder.AddRedisDistributedCache("redis");
		builder.AddRedisClient("redis");

		builder.Services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
		});

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

		// Domain events: dispatched in-process (same transaction boundary).
		// Integration events: published via Wolverine/RabbitMQ (cross-instance).
		// AddInProcessEventBus() is intentionally not called here — Wolverine
		// owns the IEventBus binding, and registering an in-process one only
		// to immediately override it is wasted DI churn.
		builder.Services.AddInProcessDomainEventDispatcher();
		builder.AddWolverineEventBus(eventHandlerAssemblies);

		builder.Services.AddScoped<DomainEventDispatchInterceptor>();
		builder.Services.AddQueryCounting();
		builder.Services.TryAddSingleton(TimeProvider.System);

		var configuration = builder.Configuration;

		builder.Services.AddOpenApi(options =>
		{
			options.AddDocumentTransformer((document, _, _) =>
			{
				document.Servers = [];

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

		var isE2ETests = configuration.GetValue<bool>("E2ETests");

		builder.Services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			options.AddPolicy(RateLimitPolicies.RecipeImport, context =>
			{
				if (isE2ETests) return RateLimitPartition.GetNoLimiter("e2e-tests");

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
				if (isE2ETests) return RateLimitPartition.GetNoLimiter("e2e-tests");

				var userId = context.User?.FindFirstValue(KeycloakClaimTypes.Subject) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
				return RedisRateLimitPartition.GetFixedWindowRateLimiter(userId, _ => new RedisFixedWindowRateLimiterOptions
				{
					PermitLimit = 60,
					Window = TimeSpan.FromMinutes(1),
					ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
				});
			});

			// Anonymous-only — partitions by client IP (resolved via ForwardedHeaders).
			// If RemoteIpAddress is somehow null, the partition key is the literal
			// "unknown" — those requests share a single global bucket of 10/min,
			// which still caps abuse from any path that bypasses the gateway.
			options.AddPolicy(RateLimitPolicies.AnonymousAuth, context =>
			{
				if (isE2ETests) return RateLimitPartition.GetNoLimiter("e2e-tests");

				var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
				return RedisRateLimitPartition.GetSlidingWindowRateLimiter(clientIp, _ => new RedisSlidingWindowRateLimiterOptions
				{
					PermitLimit = 10,
					Window = TimeSpan.FromMinutes(1),
					ConnectionMultiplexerFactory = () => context.RequestServices.GetRequiredService<IConnectionMultiplexer>(),
				});
			});
		});

		// Trust X-Forwarded-* from the immediate proxy (Yumney.Gateway). The gateway sits
		// at a private/loopback address that varies per environment (loopback in Aspire
		// dev, a Container Apps internal IP in Azure), so we trust any direct caller and
		// rely on Container Apps' internal-only ingress to ensure the API is unreachable
		// except via the gateway. ForwardLimit = 1 means only the gateway's claim is
		// honoured; chained X-Forwarded-For values further upstream are discarded.
		builder.Services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
			options.ForwardLimit = 1;
			options.KnownIPNetworks.Clear();
			options.KnownProxies.Clear();
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("0.0.0.0/0"));
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("::/0"));
		});

		builder.Services.AddHttpClient();
		builder.Services.AddHealthChecks()
			.AddCheck<HealthChecks.KeycloakHealthCheck>("keycloak", tags: ["ready"])
			.AddCheck<HealthChecks.RedisHealthCheck>("redis", tags: ["ready"]);

		return builder;
	}

	public static WebApplication UseYumneyDefaults(this WebApplication app)
	{
		app.UseForwardedHeaders();
		app.UseMiddleware<CorrelationIdMiddleware>();
		app.UseMiddleware<RequestLoggingMiddleware>();
		app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

		if (!app.Environment.IsDevelopment())
		{
			app.UseHsts();
		}

		app.UseHttpsRedirection();
		app.UseResponseCompression();

		app.UseAuthentication()
			.UseAuthorization();

		app.UseMiddleware<RequestContextMiddleware>();

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
