using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Auth;

/// <summary>
/// JWT bearer wiring against Keycloak — mirrors the pattern in
/// <c>Yumney.Shared.Web.HostBuilderExtensions.AddYumneyDefaults</c> but
/// inlined here to keep the MCP server's dependency footprint small (no
/// Wolverine, no Persistence, no EF Core).
/// </summary>
#pragma warning disable SA1303
public static class KeycloakAuthExtensions
{
	private const string realmConfigKey = "Keycloak:Realm";
	private const string keycloakConnectionStringName = "keycloak";
	private const string defaultRealm = "yumney";
	private const string defaultUrl = "http://localhost:8080";
	private const string audience = "yumney-api";

	/// <summary>Add JWT bearer auth pointing at the Aspire-provisioned Keycloak realm.</summary>
	/// <param name="services">Service collection.</param>
	/// <param name="configuration">Configuration source.</param>
	/// <param name="environment">Host environment — controls dev-mode laxity.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddKeycloakBearerAuthentication(
		this IServiceCollection services,
		IConfiguration configuration,
		IHostEnvironment environment)
	{
		var isDevelopment = environment.IsDevelopment();

		services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.Authority = RealmUrl(configuration);
				options.Audience = audience;
				options.RequireHttpsMetadata = !isDevelopment;
				options.TokenValidationParameters.ValidateIssuer = !isDevelopment;
				if (!isDevelopment)
				{
					options.TokenValidationParameters.ValidIssuer = RealmUrl(configuration);
				}
			});

		services.AddAuthorization();
		return services;
	}

	private static string GetBaseUrl(IConfiguration configuration) =>
		configuration.GetConnectionString(keycloakConnectionStringName)
			?? configuration[$"services:{keycloakConnectionStringName}:http:0"]
			?? defaultUrl;

	private static string GetRealm(IConfiguration configuration) =>
		configuration.GetValue<string>(realmConfigKey) ?? defaultRealm;

	private static string RealmUrl(IConfiguration configuration) =>
		$"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}";
}
