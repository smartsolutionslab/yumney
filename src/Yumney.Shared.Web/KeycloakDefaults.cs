using Microsoft.Extensions.Configuration;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class KeycloakDefaults
{
	public const string RealmConfigKey = "Keycloak:Realm";
	public const string ConnectionStringName = "keycloak";
	public const string DefaultRealm = "yumney";
	public const string DefaultUrl = "http://localhost:8080";
	public const string Audience = "yumney-api";
	public const string WebClientId = "yumney-web";

	public const string ScopeOpenId = "openid";
	public const string ScopeProfile = "profile";

	public static string GetBaseUrl(IConfiguration configuration)
	{
		// Aspire 13.3.3 may publish Keycloak under services:keycloak:https:0
		// (HTTPS upgrade for dev-cert scenarios) rather than :http:0; accept either.
		return configuration.GetConnectionString(ConnectionStringName)
			?? configuration[$"services:{ConnectionStringName}:http:0"]
			?? configuration[$"services:{ConnectionStringName}:https:0"]
			?? DefaultUrl;
	}

	public static string GetRealm(IConfiguration configuration) => configuration.GetValue<string>(RealmConfigKey) ?? DefaultRealm;

	public static string AuthorizationUrl(IConfiguration configuration) => $"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}/protocol/openid-connect/auth";

	public static string TokenUrl(IConfiguration configuration) => $"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}/protocol/openid-connect/token";

	public static string RealmUrl(IConfiguration configuration) => $"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}";
}
