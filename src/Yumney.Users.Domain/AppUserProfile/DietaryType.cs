using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311 // editorconfig requires camelCase for private fields
public sealed record DietaryType : IValueObject<string>
{
	public const int MaxLength = 20;

#pragma warning disable SA1202 // allowedValues must initialize before the public static instances
	private static readonly string[] allowedValues =
		["omnivore", "vegetarian", "vegan", "pescatarian", "flexitarian"];

	public static readonly DietaryType Omnivore = new("omnivore");
	public static readonly DietaryType Vegetarian = new("vegetarian");
	public static readonly DietaryType Vegan = new("vegan");
	public static readonly DietaryType Pescatarian = new("pescatarian");
	public static readonly DietaryType Flexitarian = new("flexitarian");
#pragma warning restore SA1202

	public string Value { get; }

	private DietaryType(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.IsOneOf(allowedValues)
			.AndReturn();
	}

	public static DietaryType From(string value) => new(value);

	public static implicit operator string(DietaryType obj) => obj.Value;
}
#pragma warning restore SA1311
