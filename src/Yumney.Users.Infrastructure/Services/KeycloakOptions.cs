using System.ComponentModel.DataAnnotations;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure;

public sealed class KeycloakOptions
{
	public const string SectionName = "Keycloak";

	[Required]
	public string Realm { get; init; } = "yumney";

	[Required]
	public string ClientId { get; init; } = "yumney-api";

	[Required(AllowEmptyStrings = false)]
	public string ClientSecret { get; init; } = string.Empty;
}
