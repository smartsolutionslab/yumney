using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311
public sealed record DietaryRestriction : IValueObject<string>
{
    public const int MaxLength = 30;

#pragma warning disable SA1202
    private static readonly string[] allowedValues =
        ["gluten-free", "lactose-free", "nut-allergy", "egg-free", "soy-free", "shellfish-allergy", "halal", "kosher"];

    public static readonly DietaryRestriction GlutenFree = new("gluten-free");
    public static readonly DietaryRestriction LactoseFree = new("lactose-free");
    public static readonly DietaryRestriction NutAllergy = new("nut-allergy");
    public static readonly DietaryRestriction EggFree = new("egg-free");
    public static readonly DietaryRestriction SoyFree = new("soy-free");
    public static readonly DietaryRestriction ShellfishAllergy = new("shellfish-allergy");
    public static readonly DietaryRestriction Halal = new("halal");
    public static readonly DietaryRestriction Kosher = new("kosher");
#pragma warning restore SA1202

    public string Value { get; }

    private DietaryRestriction(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsOneOf(allowedValues)
            .AndReturn();
    }

    public static DietaryRestriction From(string value) => new(value);

    public static IReadOnlyList<string> AllowedValues => allowedValues;

    public static implicit operator string(DietaryRestriction obj) => obj.Value;
}
#pragma warning restore SA1311
