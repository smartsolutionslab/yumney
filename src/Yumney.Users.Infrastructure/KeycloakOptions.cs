namespace SmartSolutionsLab.Yumney.Users.Infrastructure;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Realm { get; init; } = "yumney";

    public string ClientId { get; init; } = "yumney-api";

    public string ClientSecret { get; init; } = string.Empty;
}
