using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record KeycloakUserId : IValueObject<string>
{
	public const int MaxLength = 255;

	public string Value { get; }

	private KeycloakUserId(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
	}

	public static KeycloakUserId From(string value) => new(value);

	public static implicit operator string(KeycloakUserId obj) => obj.Value;
}
