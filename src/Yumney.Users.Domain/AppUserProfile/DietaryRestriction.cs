using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record DietaryRestriction : IValueObject<string>
{
	public const int MaxLength = 30;
	public static readonly DietaryRestriction GlutenFree = new("gluten-free");
	public static readonly DietaryRestriction LactoseFree = new("lactose-free");
	public static readonly DietaryRestriction NutAllergy = new("nut-allergy");
	public static readonly DietaryRestriction EggFree = new("egg-free");
	public static readonly DietaryRestriction SoyFree = new("soy-free");
	public static readonly DietaryRestriction ShellfishAllergy = new("shellfish-allergy");
	public static readonly DietaryRestriction Halal = new("halal");
	public static readonly DietaryRestriction Kosher = new("kosher");

#pragma warning disable SA1311
	private static readonly string[] allowedValues =
	[
		GlutenFree.Value,
		LactoseFree.Value,
		NutAllergy.Value,
		EggFree.Value,
		SoyFree.Value,
		ShellfishAllergy.Value,
		Halal.Value,
		Kosher.Value
	];
#pragma warning restore SA1311

	public string Value { get; }

	private DietaryRestriction(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
	}

	public static DietaryRestriction From(string value)
	{
		Ensure.That(value).IsNotNullOrWhiteSpace().HasMaxLength(MaxLength).IsOneOf(allowedValues);
		return new DietaryRestriction(value);
	}

	public static IReadOnlyList<string> AllowedValues => allowedValues;

	public static implicit operator string(DietaryRestriction obj) => obj.Value;
}
