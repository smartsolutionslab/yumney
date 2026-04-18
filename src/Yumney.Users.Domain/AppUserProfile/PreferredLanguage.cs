using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311 // editorconfig requires camelCase for private fields
public sealed record PreferredLanguage : IValueObject<string>
{
	public const int MaxLength = 10;

#pragma warning disable SA1202 // allowedValues must initialize before the public static instances
	private static readonly string[] allowedValues = ["en", "de"];

	public static readonly PreferredLanguage English = new("en");
	public static readonly PreferredLanguage German = new("de");
#pragma warning restore SA1202

	public string Value { get; }

	private PreferredLanguage(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.IsOneOf(allowedValues)
			.AndReturn();
	}

	public static PreferredLanguage From(string value) => new(value);

	public static implicit operator string(PreferredLanguage obj) => obj.Value;
}
#pragma warning restore SA1311
