using Microsoft.AspNetCore.Authentication.JwtBearer;

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
		var realmName = GetRealm(configuration);
		var publicResourceUrl = configuration.GetValue<string>("McpServer:PublicUrl");

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

				// Aspire 13.3.3+ may publish Keycloak as HTTPS in dev with a self-signed
				// cert. OIDC discovery + JWKS fetch over that URL needs to skip TLS validation.
				if (isDevelopment)
				{
					options.BackchannelHttpHandler = new HttpClientHandler
					{
						ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
					};
				}

				options.Events = new JwtBearerEvents
				{
					OnChallenge = challenge =>
					{
						// Override the default WWW-Authenticate header so MCP clients
						// (Claude.ai, ChatGPT custom GPTs) can find the RFC 9728
						// discovery document from a 401 response alone — see RFC 6750 §3.
						challenge.HandleResponse();
						var discoveryUrl = WwwAuthenticateChallenge.ResolveDiscoveryUrl(challenge.Request, publicResourceUrl);
						var error = challenge.AuthenticateFailure is not null ? "invalid_token" : null;
						challenge.Response.StatusCode = StatusCodes.Status401Unauthorized;
						challenge.Response.Headers.WWWAuthenticate = WwwAuthenticateChallenge.BuildHeader(realmName, discoveryUrl, error);
						return Task.CompletedTask;
					},
				};
			});

		services.AddAuthorization();
		return services;
	}

	/// <summary>
	/// Resolves the public Keycloak realm URL (e.g. <c>http://localhost:8080/realms/yumney</c>)
	/// from the same configuration the bearer authentication uses. Exposed so the OAuth
	/// protected-resource discovery endpoint can advertise it to MCP clients without
	/// re-implementing the resolution logic.
	/// </summary>
	/// <param name="configuration">Configuration source.</param>
	/// <returns>The canonical realm URL.</returns>
	public static string ResolveRealmUrl(IConfiguration configuration) => RealmUrl(configuration);

	private static string GetBaseUrl(IConfiguration configuration) =>
		configuration.GetConnectionString(keycloakConnectionStringName)
			?? configuration[$"services:{keycloakConnectionStringName}:http:0"]
			?? configuration[$"services:{keycloakConnectionStringName}:https:0"]
			?? defaultUrl;

	private static string GetRealm(IConfiguration configuration) =>
		configuration.GetValue<string>(realmConfigKey) ?? defaultRealm;

	private static string RealmUrl(IConfiguration configuration) =>
		$"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}";
}
