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

    public static string GetBaseUrl(IConfiguration configuration)
    {
        return configuration.GetConnectionString(ConnectionStringName) ?? DefaultUrl;
    }

    public static string GetRealm(IConfiguration configuration)
    {
        return configuration.GetValue<string>(RealmConfigKey) ?? DefaultRealm;
    }

    public static string AuthorizationUrl(IConfiguration configuration)
    {
        return $"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}/protocol/openid-connect/auth";
    }

    public static string TokenUrl(IConfiguration configuration)
    {
        return $"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}/protocol/openid-connect/token";
    }

    public static string RealmUrl(IConfiguration configuration)
    {
        return $"{GetBaseUrl(configuration)}/realms/{GetRealm(configuration)}";
    }
}
