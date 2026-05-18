using System.IO.Compression;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using SmartSolutionsLab.Yumney.Shared.Web.Services;

namespace SmartSolutionsLab.Yumney.Shared.Web;

/// <summary>
/// Composition-root wiring shared by every Yumney module host (Recipes, Shopping,
/// MealPlan, Users). <see cref="AddYumneyDefaults"/> orchestrates per-concern
/// helpers (auth, event bus, OpenAPI, compression, rate limiting, forwarded
/// headers); <see cref="UseYumneyDefaults"/> wires the matching middleware
/// pipeline.
/// </summary>
public static partial class HostBuilderExtensions
{
	public static WebApplicationBuilder AddYumneyDefaults(
		this WebApplicationBuilder builder,
		string outboxConnectionName,
		string outboxSchema,
		params Assembly[] eventHandlerAssemblies)
	{
		builder.AddServiceDefaults();
		builder.AddRedisDistributedCache("redis");
		builder.AddRedisClient("redis");

		builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

		builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30));

		builder.Services.AddHttpContextAccessor();
		builder.Services.AddScoped<ICurrentUser, CurrentUserProvider>();
		builder.Services.AddQueryCounting();
		builder.Services.TryAddSingleton(TimeProvider.System);
		builder.Services.AddHttpClient();
		builder.Services.AddHealthChecks()
			.AddCheck<HealthChecks.KeycloakHealthCheck>("keycloak", tags: ["ready"])
			.AddCheck<HealthChecks.RedisHealthCheck>("redis", tags: ["ready"]);

		return builder
			.AddYumneyAuthentication()
			.AddYumneyEventBus(outboxConnectionName, outboxSchema, eventHandlerAssemblies)
			.AddYumneyOpenApi()
			.AddYumneyResponseCompression()
			.AddYumneyRateLimiting()
			.AddYumneyForwardedHeaders();
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
			app.MapScalarApiReference(options => options
				.WithTitle("Yumney API")
				.WithTheme(ScalarTheme.Saturn)
				.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
				.AddAuthorizationCodeFlow("keycloak", flow => flow
					.WithClientId(KeycloakDefaults.WebClientId)
					.WithAuthorizationUrl(KeycloakDefaults.AuthorizationUrl(app.Configuration))
					.WithTokenUrl(KeycloakDefaults.TokenUrl(app.Configuration))
					.WithSelectedScopes([KeycloakDefaults.ScopeOpenId, KeycloakDefaults.ScopeProfile])));
		}

		app.MapDefaultEndpoints();

		app.MapGet("/version", () => new
		{
			Version = typeof(HostBuilderExtensions).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
			Environment = app.Environment.EnvironmentName,
		}).AllowAnonymous();

		return app;
	}

	private static WebApplicationBuilder AddYumneyAuthentication(this WebApplicationBuilder builder)
	{
		// Outside Development we require HTTPS metadata and validate the
		// token issuer against the configured Keycloak realm URL. Development
		// keeps both relaxed because the Aspire Keycloak runs on plain HTTP
		// and its discovery-doc issuer can differ from the URL we call
		// (container hostname vs localhost).
		var isDevelopment = builder.Environment.IsDevelopment();
		builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.Authority = KeycloakDefaults.RealmUrl(builder.Configuration);
				options.Audience = KeycloakDefaults.Audience;
				options.RequireHttpsMetadata = !isDevelopment;
				options.TokenValidationParameters.ValidateIssuer = !isDevelopment;
				if (!isDevelopment)
				{
					options.TokenValidationParameters.ValidIssuer = KeycloakDefaults.RealmUrl(builder.Configuration);
				}

				// Aspire 13.3.3+ may publish Keycloak as HTTPS in dev with a self-signed
				// cert. The OIDC discovery / JWKS fetch happens over this same URL, so
				// skip backchannel TLS validation in Development.
				if (isDevelopment)
				{
					options.BackchannelHttpHandler = new HttpClientHandler
					{
						ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
					};
				}
			});

		builder.Services.AddAuthorization();
		return builder;
	}

	private static WebApplicationBuilder AddYumneyEventBus(
		this WebApplicationBuilder builder,
		string outboxConnectionName,
		string outboxSchema,
		Assembly[] eventHandlerAssemblies)
	{
		// Domain events: dispatched in-process (same transaction boundary).
		// Integration events: published via Wolverine/RabbitMQ (cross-instance).
		// AddInProcessEventBus() is intentionally not called here — Wolverine
		// owns the IEventBus binding, and registering an in-process one only
		// to immediately override it is wasted DI churn.
		builder.Services.AddInProcessDomainEventDispatcher();
		builder.AddWolverineEventBus(outboxConnectionName, outboxSchema, eventHandlerAssemblies);

		// Singleton — required because Wolverine.EntityFrameworkCore's
		// AddDbContextWithWolverineIntegration builds DbContextOptions
		// against the root provider, which can't resolve scoped services
		// when interceptors are added inside the options-builder delegate.
		// The interceptor itself opens a per-SaveChanges scope to dispatch
		// domain events, so handlers remain scoped.
		builder.Services.TryAddSingleton<DomainEventDispatchInterceptor>();
		return builder;
	}

	private static WebApplicationBuilder AddYumneyOpenApi(this WebApplicationBuilder builder)
	{
		var configuration = builder.Configuration;
		builder.Services.AddOpenApi(options =>
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
			}));

		return builder;
	}

	private static WebApplicationBuilder AddYumneyResponseCompression(this WebApplicationBuilder builder)
	{
		builder.Services.AddResponseCompression(options =>
		{
			options.EnableForHttps = true;
			options.Providers.Add<BrotliCompressionProvider>();
			options.Providers.Add<GzipCompressionProvider>();
		});
		builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
		builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize);
		return builder;
	}

	// Trust X-Forwarded-* only from the immediate proxy (Yumney.Gateway). The gateway
	// sits at a private/loopback address that varies per environment (loopback in
	// Aspire dev, a Container Apps internal IP in Azure), so we trust the standard
	// private-network ranges plus loopback. Trusting 0.0.0.0/0 would let any caller
	// that reaches the API directly spoof X-Forwarded-For and bypass the per-IP
	// rate limit on anonymous endpoints (e.g. Register, ResendVerification).
	// ForwardLimit = 1 means only the gateway's claim is honoured; chained
	// X-Forwarded-For values further upstream are discarded.
	private static WebApplicationBuilder AddYumneyForwardedHeaders(this WebApplicationBuilder builder)
	{
		builder.Services.Configure<ForwardedHeadersOptions>(options =>
		{
			options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
			options.ForwardLimit = 1;
			options.KnownIPNetworks.Clear();
			options.KnownProxies.Clear();
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.0/8"));
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("172.16.0.0/12"));
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("192.168.0.0/16"));
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("::1/128"));
			options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("fc00::/7"));
		});
		return builder;
	}
}
