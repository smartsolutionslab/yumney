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

    private static readonly string[] AllValues =
        [GlutenFree, LactoseFree, NutAllergy, EggFree, SoyFree, ShellfishAllergy, Halal, Kosher];

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
        Ensure.That(value).IsOneOf(AllValues);
        return new(value);
    }

    public static IReadOnlyList<string> AllowedValues => AllValues;

    public static implicit operator string(DietaryRestriction obj) => obj.Value;
}
