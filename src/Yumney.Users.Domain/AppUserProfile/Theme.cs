using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311
public sealed record Theme : IValueObject<string>
{
	public const int MaxLength = 10;

#pragma warning disable SA1202
	private static readonly string[] allowedValues = ["light", "dark", "system"];

	public static readonly Theme Light = new("light");
	public static readonly Theme Dark = new("dark");
	public static readonly Theme System = new("system");
#pragma warning restore SA1202

	public string Value { get; }

	private Theme(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.IsOneOf(allowedValues)
			.AndReturn();
	}

	public static Theme From(string value) => new(value);

	public static implicit operator string(Theme obj) => obj.Value;
}
#pragma warning restore SA1311
